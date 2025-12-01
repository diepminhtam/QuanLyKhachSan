using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLiKhachSan.Data;
using QuanLiKhachSan.Models;
using QuanLiKhachSan.ViewModels.Booking;
using BookingItemViewModel = QuanLiKhachSan.ViewModels.Booking.BookingItemViewModel;

namespace QuanLiKhachSan.Controllers
{
    [Authorize]
    public class BookingsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<BookingsController> _logger;

        public BookingsController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            ILogger<BookingsController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        // Hiển thị trang điền thông tin đặt phòng
        [HttpGet]
        public async Task<IActionResult> Create(int roomId, DateTime? checkin, DateTime? checkout, int guests = 1)
        {
            try
            {
                // Lấy thông tin user đang đăng nhập
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return RedirectToAction("Login", "Account");
                }

                // Tách tên từ FullName
                var nameParts = user.FullName?.Split(' ') ?? new[] { "", "" };
                var firstName = nameParts.Length > 0 ? nameParts[0] : "";
                var lastName = nameParts.Length > 1 ? string.Join(" ", nameParts.Skip(1)) : "";

                // Lấy thông tin phòng từ database
                var room = await _context.Rooms.FindAsync(roomId);
                if (room == null)
                {
                    return NotFound();
                }

                // Tính toán số đêm và tổng tiền
                var checkInDate = checkin ?? DateTime.Today;
                var checkOutDate = checkout ?? DateTime.Today.AddDays(2);
                var nights = (checkOutDate - checkInDate).Days;
                nights = nights > 0 ? nights : 1;

                // Tính tổng giá với phí dịch vụ và thuế
                var roomTotal = room.PricePerNight * nights;
                var serviceFee = roomTotal * 0.05m;
                var tax = roomTotal * 0.1m;
                var totalPrice = roomTotal + serviceFee + tax;

                var model = new BookingViewModel
                {
                    RoomId = roomId,
                    RoomName = room.Name,
                    RoomType = room.RoomType,
                    RoomImageUrl = room.ImageUrl,
                    CheckInDate = checkInDate,
                    CheckOutDate = checkOutDate,
                    GuestCount = guests,
                    MaxGuests = room.Capacity,
                    RoomPrice = room.PricePerNight,
                    Discount = 0,
                    TotalPrice = totalPrice,
                    FirstName = firstName,
                    LastName = lastName,
                    Email = user.Email ?? "",
                    Phone = user.PhoneNumber ?? ""
                };

                // Kiểm tra tính khả dụng của phòng cho khoảng ngày được chọn
                model.IsAvailable = await IsRoomAvailableAsync(roomId, model.CheckInDate, model.CheckOutDate);

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi hiển thị trang đặt phòng");
                TempData["Error"] = "Có lỗi xảy ra khi tải trang đặt phòng";
                return RedirectToAction("Index", "Home");
            }
        }

        // Xử lý khi người dùng nhấn "Hoàn tất đặt phòng"
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(BookingViewModel model)
        {
            // Log raw form value and model binding for AcceptTerms (informational)
            try
            {
                string? raw = null;
                if (Request != null && Request.Form != null && Request.Form.ContainsKey("AcceptTerms"))
                {
                    raw = Request.Form["AcceptTerms"].ToString();
                }
                _logger.LogInformation("BookingsController.Create POST: Request.Form[AcceptTerms]={Raw}, model.AcceptTerms={ModelAccept}", raw, model.AcceptTerms);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to log AcceptTerms debug info");
            }

            if (!ModelState.IsValid)
            {
                var errors = ModelState
                    .Where(kv => kv.Value?.Errors != null && kv.Value.Errors.Count > 0)
                    .ToDictionary(kv => kv.Key, kv => kv.Value.Errors.Select(e => e.ErrorMessage).ToArray());

                _logger.LogWarning("ModelState không hợp lệ: {ErrorsJson}", System.Text.Json.JsonSerializer.Serialize(errors));
                await ReloadRoomInfo(model);
                return View(model);
            }

            try
            {
                // 1. Kiểm tra phòng tồn tại
                var room = await _context.Rooms.FindAsync(model.RoomId);
                if (room == null)
                {
                    ModelState.AddModelError("", "Phòng không tồn tại");
                    await ReloadRoomInfo(model);
                    return View(model);
                }

                // 2. Kiểm tra số khách
                if (model.GuestCount > room.Capacity)
                {
                    ModelState.AddModelError("GuestCount", $"Số khách không được vượt quá {room.Capacity} người");
                    await ReloadRoomInfo(model);
                    return View(model);
                }

                // 3. Bắt đầu transaction và re-check availability để tránh race condition
                using (var tx = await _context.Database.BeginTransactionAsync())
                {
                    var isAvailable = await IsRoomAvailableAsync(model.RoomId, model.CheckInDate, model.CheckOutDate);
                    model.IsAvailable = isAvailable;
                    if (!isAvailable)
                    {
                        ModelState.AddModelError("", "Phòng đã được đặt trong khoảng thời gian này. Vui lòng chọn ngày khác.");
                        await ReloadRoomInfo(model);
                        return View(model);
                    }

                    // 4. Tính toán tổng giá chính xác tại thời điểm đặt
                    var nights = (model.CheckOutDate - model.CheckInDate).Days;
                    nights = nights > 0 ? nights : 1;
                    var roomTotal = room.PricePerNight * nights;
                    var serviceFee = roomTotal * 0.05m;
                    var tax = roomTotal * 0.1m;
                    var totalPrice = roomTotal + serviceFee + tax - model.Discount;

                    // 5. Lấy user
                    var user = await _userManager.GetUserAsync(User);
                    if (user == null)
                    {
                        return RedirectToAction("Login", "Account");
                    }

                    // 6. Lấy trạng thái booking (fallback nếu thiếu)
                    var bookingStatus = await _context.BookingStatuses.FirstOrDefaultAsync(bs => bs.Name == "Chờ xác nhận")
                                        ?? await _context.BookingStatuses.FirstOrDefaultAsync();

                    if (bookingStatus == null)
                    {
                        ModelState.AddModelError("", "Lỗi hệ thống. Vui lòng thử lại.");
                        await ReloadRoomInfo(model);
                        return View(model);
                    }

                    // 7. Tạo và lưu booking
                    var booking = new Booking
                    {
                        RoomId = model.RoomId,
                        CheckIn = model.CheckInDate,
                        CheckOut = model.CheckOutDate,
                        Guests = model.GuestCount,
                        TotalPrice = totalPrice,
                        CreatedDate = DateTime.UtcNow,
                        BookingStatusId = bookingStatus.Id,
                        UserId = user.Id
                    };

                    _context.Bookings.Add(booking);
                    // Save once to generate booking.Id
                    await _context.SaveChangesAsync();

                    // 8. Ghi Payment (nếu cần). Ở đây mặc định tạo bản ghi 'Chờ thanh toán' nếu tồn tại
                    var pendingPaymentStatus = await _context.PaymentStatuses.FirstOrDefaultAsync(ps => ps.Name == "Chờ thanh toán")
                                               ?? await _context.PaymentStatuses.FirstOrDefaultAsync();

                    if (pendingPaymentStatus != null)
                    {
                        var payment = new Payment
                        {
                            BookingId = booking.Id,
                            Amount = booking.TotalPrice,
                            PaymentDate = DateTime.UtcNow,
                            PaymentStatusId = pendingPaymentStatus.Id
                        };

                        _context.Payments.Add(payment);
                    }

                    // 9. Ghi lịch sử trạng thái đặt phòng
                    var history = new BookingStatusHistory
                    {
                        BookingId = booking.Id,
                        FromBookingStatusId = null,
                        ToBookingStatusId = bookingStatus.Id,
                        ChangedByUserId = user.Id,
                        Note = "Tạo đặt phòng bởi khách",
                        ChangedAt = DateTime.UtcNow
                    };

                    _context.Add(history);

                    await _context.SaveChangesAsync();
                    await tx.CommitAsync();

                    TempData["Success"] = "Đặt phòng thành công. Mã đơn: BK" + booking.Id.ToString("D6");
                    _logger.LogInformation("Booking created {BookingId} for user {UserId}", booking.Id, user.Id);

                    return RedirectToAction("Confirmation", new { id = booking.Id });
                }
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Database error while creating booking");
                ModelState.AddModelError("", "Lỗi cơ sở dữ liệu khi đặt phòng. Vui lòng thử lại.");
                await ReloadRoomInfo(model);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while creating booking");
                ModelState.AddModelError("", "Có lỗi xảy ra khi đặt phòng. Vui lòng thử lại.");
                await ReloadRoomInfo(model);
                return View(model);
            }
        }

        // Kiểm tra phòng có trống không
        private async Task<bool> IsRoomAvailableAsync(int roomId, DateTime checkIn, DateTime checkOut)
        {
            // Lấy id của trạng thái 'Đã hủy' (nếu có)
            var cancelledStatus = await _context.BookingStatuses
                .AsNoTracking()
                .FirstOrDefaultAsync(bs => bs.Name == "Đã hủy");

            var cancelledId = cancelledStatus?.Id;

            // Logic kiểm tra overlap: hai khoảng chồng lên nhau khi
            // (checkIn < existing.CheckOut) && (checkOut > existing.CheckIn)
            var query = _context.Bookings.AsQueryable();
            query = query.Where(b => b.RoomId == roomId);

            if (cancelledId.HasValue)
            {
                query = query.Where(b => b.BookingStatusId != cancelledId.Value);
            }

            var hasConflict = await query.AnyAsync(b => checkIn < b.CheckOut && checkOut > b.CheckIn);

            return !hasConflict;
        }

        private async Task ReloadRoomInfo(BookingViewModel model)
        {
            if (model.RoomId <= 0) return;

            try
            {
                var room = await _context.Rooms
                    .AsNoTracking()
                    .Where(r => r.Id == model.RoomId)
                    .Select(r => new { r.Name, r.RoomType, r.ImageUrl, r.Capacity, r.PricePerNight })
                    .FirstOrDefaultAsync();

                if (room != null)
                {
                    model.RoomName = room.Name;
                    model.RoomType = room.RoomType;
                    model.RoomImageUrl = room.ImageUrl;
                    model.MaxGuests = room.Capacity;
                    model.RoomPrice = room.PricePerNight;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error reloading room info for room {RoomId}", model.RoomId);
            }
        }

        // Trang xác nhận đặt phòng
        public async Task<IActionResult> Confirmation(int id)
        {
            try
            {
                var booking = await _context.Bookings
                    .Include(b => b.Room)
                    .Include(b => b.BookingStatus)
                    .Include(b => b.User)
                    .Include(b => b.Payments).ThenInclude(p => p.PaymentStatus)
                    .FirstOrDefaultAsync(b => b.Id == id);

                if (booking == null)
                {
                    _logger.LogWarning("Booking not found: {BookingId}", id);
                    return NotFound();
                }

                // Kiểm tra quyền truy cập
                var currentUser = await _userManager.GetUserAsync(User);
                if (booking.UserId != currentUser?.Id && !User.IsInRole("Admin"))
                {
                    _logger.LogWarning("User {UserId} attempted to access booking {BookingId} without permission",
                        currentUser?.Id, id);
                    return Forbid();
                }

                // Chuyển đổi sang ViewModel
                var viewModel = new BookingConfirmationViewModel
                {
                    BookingId = booking.Id,
                    BookingNumber = $"BK{booking.Id:D6}",
                    CustomerName = booking.User?.FullName ?? "Khách hàng",
                    RoomName = booking.Room?.Name ?? "Không xác định",
                    CheckInDate = booking.CheckIn,
                    CheckOutDate = booking.CheckOut,
                    GuestCount = booking.Guests,
                    TotalPrice = booking.TotalPrice,
                    Status = booking.BookingStatus?.Name ?? "Chờ xác nhận",
                    BookingDate = booking.CreatedDate
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading confirmation page for booking {BookingId}", id);
                TempData["Error"] = "Có lỗi xảy ra khi tải trang xác nhận";
                return RedirectToAction("Index", "Home");
            }
        }

        // Trang Lịch sử/Danh sách đặt phòng
        public async Task<IActionResult> Index(string status = null)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return RedirectToAction("Login", "Account");
                }

                var userBookings = await _context.Bookings
                    .Include(b => b.Room)
                    .Include(b => b.BookingStatus)
                    .Include(b => b.Review)
                    .Where(b => b.UserId == user.Id)
                    .OrderByDescending(b => b.CreatedDate)
                    .ToListAsync();

                var bookings = userBookings.Select(b => new BookingItemViewModel
                {
                    Id = b.Id,
                    BookingNumber = $"BK{b.Id:D6}",
                    RoomName = b.Room?.Name ?? "Unknown Room",
                    RoomImageUrl = b.Room?.ImageUrl ?? "/images/rooms/default.jpg",
                    CheckInDate = b.CheckIn,
                    CheckOutDate = b.CheckOut,
                    GuestsCount = b.Guests,
                    TotalPrice = b.TotalPrice,
                    Status = GetBookingStatusDisplay(b.BookingStatus?.Name),
                    BookingDate = b.CreatedDate,
                    HasReview = b.Review != null
                }).ToList();

                var viewModel = new BookingListViewModel
                {
                    Status = status,
                    Bookings = bookings
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading bookings list for user");
                TempData["Error"] = "Có lỗi xảy ra khi tải danh sách đặt phòng";
                return RedirectToAction("Index", "Home");
            }
        }

        // Trang chi tiết booking
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var booking = await _context.Bookings
                    .Include(b => b.Room)
                    .Include(b => b.BookingStatus)
                    .Include(b => b.User)
                    .FirstOrDefaultAsync(b => b.Id == id);

                if (booking == null)
                {
                    return NotFound();
                }

                // Kiểm tra quyền truy cập
                var currentUser = await _userManager.GetUserAsync(User);
                if (booking.UserId != currentUser?.Id && !User.IsInRole("Admin"))
                {
                    return Forbid();
                }

                // Calculate price breakdown and populate a richer view model
                var nights = (booking.CheckOut - booking.CheckIn).Days;
                nights = nights > 0 ? nights : 1;

                var roomPriceTotal = booking.Room != null ? (booking.Room.PricePerNight * nights) : 0m;
                var serviceFee = Math.Round(roomPriceTotal * 0.05m, 0);
                var taxFee = Math.Round(roomPriceTotal * 0.1m, 0);

                // If booking.TotalPrice differs, compute discount as the difference (safe fallback)
                var expectedTotal = roomPriceTotal + serviceFee + taxFee;
                var discount = 0m;
                if (booking.TotalPrice < expectedTotal)
                {
                    discount = expectedTotal - booking.TotalPrice;
                }

                var payment = booking.Payments?.OrderByDescending(p => p.PaymentDate).FirstOrDefault();
                var paymentStatus = payment?.PaymentStatus?.Name ?? (booking.Payments != null && booking.Payments.Any() ? "Đã thanh toán" : "Chưa thanh toán");
                var paymentMethod = string.Empty;

                var statusName = (booking.BookingStatus?.Name ?? string.Empty).ToLower();
                var isCancelled = statusName.Contains("hủy") || statusName.Contains("cancel");
                var isConfirmed = statusName.Contains("xác nhận") || statusName.Contains("confirmed");

                var viewModel = new BookingDetailsViewModel
                {
                    Id = booking.Id,
                    BookingNumber = $"BK{booking.Id:D6}",
                    Status = GetBookingStatusDisplay(booking.BookingStatus?.Name),
                    BookingDate = booking.CreatedDate,
                    RoomId = booking.RoomId,
                    RoomName = booking.Room?.Name ?? "Không xác định",
                    RoomType = booking.Room?.RoomType ?? "",
                    RoomDescription = booking.Room?.Description ?? string.Empty,
                    RoomImageUrl = booking.Room?.ImageUrl ?? "/images/rooms/room-placeholder.svg",
                    RoomRating = booking.Room?.AverageRating ?? 0,
                    RoomCapacity = booking.Room?.Capacity ?? 1,
                    CheckInDate = booking.CheckIn,
                    CheckOutDate = booking.CheckOut,
                    GuestsCount = booking.Guests,
                    GuestName = currentUser?.FullName ?? "Khách hàng",
                    GuestEmail = currentUser?.Email ?? string.Empty,
                    GuestPhone = currentUser?.PhoneNumber ?? string.Empty,
                    RoomPrice = roomPriceTotal,
                    ServiceFee = serviceFee,
                    TaxFee = taxFee,
                    Discount = discount,
                    TotalPrice = booking.TotalPrice,
                    RoomSize = 0,
                    BedType = string.Empty,
                    Floor = string.Empty,
                    Building = string.Empty,
                    PaymentMethod = string.Empty,
                    PaymentStatus = paymentStatus,
                    PaymentDetails = string.Empty,
                    IsCancellable = !isCancelled && (!isConfirmed ? booking.CheckIn > DateTime.Now : booking.CheckIn > DateTime.Now.AddHours(24)),
                    FreeCancellationDeadline = booking.CheckIn.AddHours(-24),
                    CanReview = booking.CheckOut < DateTime.Now && booking.Review == null,
                    HasReview = booking.Review != null
                };

                // Attach booking activities (status history + payments)
                var histories = await _context.BookingStatusHistories
                    .Where(h => h.BookingId == id)
                    .Include(h => h.FromBookingStatus)
                    .Include(h => h.ToBookingStatus)
                    .OrderByDescending(h => h.ChangedAt)
                    .ToListAsync();

                foreach (var h in histories)
                {
                    var type = "update";
                    var toName = h.ToBookingStatus?.Name ?? string.Empty;
                    if (toName.Contains("xác nhận") || toName.Contains("Đã xác nhận") || toName.Contains("confirmed")) type = "confirm";
                    if (toName.Contains("hủy") || toName.Contains("Đã hủy") || toName.Contains("cancel")) type = "cancel";

                    viewModel.BookingActivities.Add(new BookingActivityViewModel
                    {
                        Date = h.ChangedAt,
                        Title = h.ToBookingStatus?.Name ?? "Trạng thái cập nhật",
                        Description = h.Note ?? $"{h.FromBookingStatus?.Name ?? "-"} → {h.ToBookingStatus?.Name ?? "-"}",
                        Type = type
                    });
                }

                // Add payments as activities
                var payments = booking.Payments?.OrderByDescending(p => p.PaymentDate) ?? Enumerable.Empty<QuanLiKhachSan.Models.Payment>();
                foreach (var p in payments)
                {
                    viewModel.BookingActivities.Add(new BookingActivityViewModel
                    {
                        Date = p.PaymentDate,
                        Title = $"Thanh toán: {p.Amount:N0} VNĐ",
                        Description = p.PaymentStatus?.Name ?? string.Empty,
                        Type = "payment"
                    });
                }

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading booking details {BookingId}", id);
                TempData["Error"] = "Có lỗi xảy ra khi tải chi tiết đặt phòng";
                return RedirectToAction("Index");
            }
        }

        // Hủy đặt phòng
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            try
            {
                var booking = await _context.Bookings
                    .Include(b => b.BookingStatus)
                    .FirstOrDefaultAsync(b => b.Id == id);

                if (booking == null)
                {
                    return NotFound();
                }

                // Kiểm tra quyền
                var user = await _userManager.GetUserAsync(User);
                if (booking.UserId != user?.Id && !User.IsInRole("Admin"))
                {
                    return Forbid();
                }

                // Kiểm tra có thể hủy không (khách chỉ được hủy khi chưa được xác nhận và chưa đến ngày check-in)
                var currentStatus = (booking.BookingStatus?.Name ?? string.Empty).ToLower();
                var alreadyCancelled = currentStatus.Contains("hủy") || currentStatus.Contains("cancel");
                var alreadyConfirmed = currentStatus.Contains("xác nhận") || currentStatus.Contains("confirmed");

                if (alreadyCancelled)
                {
                    TempData["Error"] = "Đặt phòng đã được hủy trước đó.";
                    return RedirectToAction("Details", new { id });
                }

                // Nếu là user thường, không cho hủy khi đã xác nhận
                if (!User.IsInRole("Admin") && alreadyConfirmed)
                {
                    TempData["Error"] = "Không thể hủy đơn đã được xác nhận. Vui lòng liên hệ quản trị để được hỗ trợ.";
                    return RedirectToAction("Details", new { id });
                }

                // Hạn chót hủy:
                // - Nếu booking đã xác nhận (confirmed) => chỉ cho hủy khi còn >24 giờ trước check-in
                // - Nếu booking chưa xác nhận (pending/other) => cho hủy miễn là chưa tới ngày nhận phòng
                if (alreadyConfirmed)
                {
                    if (booking.CheckIn <= DateTime.Now.AddHours(24))
                    {
                        TempData["Error"] = "Chỉ có thể hủy đơn đã xác nhận nếu còn hơn 24 giờ trước giờ nhận phòng.";
                        return RedirectToAction("Details", new { id });
                    }
                }
                else
                {
                    if (booking.CheckIn <= DateTime.Now)
                    {
                        TempData["Error"] = "Không thể hủy đặt phòng khi đã đến hoặc đã qua ngày nhận phòng.";
                        return RedirectToAction("Details", new { id });
                    }
                }

                // Lấy trạng thái "Đã hủy"
                var cancelledStatus = await _context.BookingStatuses
                    .FirstOrDefaultAsync(bs => bs.Name == "Đã hủy");

                if (cancelledStatus != null)
                {
                    booking.BookingStatusId = cancelledStatus.Id;
                    await _context.SaveChangesAsync();

                    TempData["Success"] = "Đã hủy đặt phòng thành công";
                }
                else
                {
                    TempData["Error"] = "Lỗi hệ thống khi hủy đặt phòng";
                }

                return RedirectToAction("Details", new { id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling booking {BookingId}", id);
                TempData["Error"] = "Có lỗi xảy ra khi hủy đặt phòng";
                return RedirectToAction("Details", new { id });
            }
        }

        private string GetBookingStatusDisplay(string? status)
        {
            return status?.ToLower() switch
            {
                "pending" or "chờ xác nhận" => "Chờ xác nhận",
                "confirmed" or "đã xác nhận" => "Đã xác nhận",
                "completed" or "hoàn thành" => "Hoàn thành",
                "cancelled" or "đã hủy" => "Đã hủy",
                _ => status ?? "Không xác định"
            };
        }
    }
}