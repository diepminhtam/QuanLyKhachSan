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
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("ModelState không hợp lệ: {@Errors}",
                    ModelState.Values.SelectMany(v => v.Errors));
                await ReloadRoomInfo(model);
                return View(model);
            }

            try
            {
                // 1. Kiểm tra ngày hợp lệ
                if (model.CheckInDate >= model.CheckOutDate)
                {
                    ModelState.AddModelError("CheckOutDate", "Ngày trả phòng phải sau ngày nhận phòng");
                    await ReloadRoomInfo(model);
                    return View(model);
                }

                // 2. Kiểm tra số khách
                if (model.GuestCount > model.MaxGuests)
                {
                    ModelState.AddModelError("GuestCount", $"Số khách không được vượt quá {model.MaxGuests} người");
                    await ReloadRoomInfo(model);
                    return View(model);
                }

                // 3. Kiểm tra phòng có tồn tại
                var room = await _context.Rooms.FindAsync(model.RoomId);
                if (room == null)
                {
                    ModelState.AddModelError("", "Phòng không tồn tại");
                    await ReloadRoomInfo(model);
                    return View(model);
                }

                // 4. Kiểm tra phòng có trống không
                var isRoomAvailable = await IsRoomAvailableAsync(model.RoomId, model.CheckInDate, model.CheckOutDate);
                if (!isRoomAvailable)
                {
                    ModelState.AddModelError("", "Phòng đã được đặt trong khoảng thời gian này. Vui lòng chọn ngày khác.");
                    await ReloadRoomInfo(model);
                    return View(model);
                }

                // 5. Lấy user info
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return RedirectToAction("Login", "Account");
                }

                // 6. Lấy booking status
                var bookingStatus = await _context.BookingStatuses
                    .FirstOrDefaultAsync(bs => bs.Name == "Chờ xác nhận");

                if (bookingStatus == null)
                {
                    // Fallback: lấy status đầu tiên
                    bookingStatus = await _context.BookingStatuses.FirstOrDefaultAsync();
                    if (bookingStatus == null)
                    {
                        ModelState.AddModelError("", "Lỗi hệ thống. Vui lòng thử lại.");
                        await ReloadRoomInfo(model);
                        return View(model);
                    }
                }

                // 7. Tính toán tổng giá
                var nights = (model.CheckOutDate - model.CheckInDate).Days;
                nights = nights > 0 ? nights : 1;
                var roomTotal = room.PricePerNight * nights;
                var serviceFee = roomTotal * 0.05m;
                var tax = roomTotal * 0.1m;
                var totalPrice = roomTotal + serviceFee + tax;

                // 8. Tạo booking
                var booking = new Booking
                {
                    RoomId = model.RoomId,
                    CheckIn = model.CheckInDate,
                    CheckOut = model.CheckOutDate,
                    Guests = model.GuestCount,
                    TotalPrice = totalPrice,
                    CreatedDate = DateTime.Now,
                    BookingStatusId = bookingStatus.Id,
                    UserId = user.Id
                };

                // 9. Lưu booking
                _context.Bookings.Add(booking);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Redirecting to Confirmation with ID: {BookingId}", booking.Id);

                // 10. Chuyển hướng sang confirmation
                return RedirectToAction("Confirmation", "Bookings", new { id = booking.Id });
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
            var conflictingBookings = await _context.Bookings
                .Where(b => b.RoomId == roomId &&
                           b.BookingStatus.Name != "Đã hủy" && // Không tính các booking đã hủy
                           ((checkIn >= b.CheckIn && checkIn < b.CheckOut) ||
                            (checkOut > b.CheckIn && checkOut <= b.CheckOut) ||
                            (checkIn <= b.CheckIn && checkOut >= b.CheckOut)))
                .CountAsync();

            return conflictingBookings == 0;
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

                var viewModel = new BookingDetailsViewModel
                {
                    Id = booking.Id,
                    BookingNumber = $"BK{booking.Id:D6}",
                    Status = GetBookingStatusDisplay(booking.BookingStatus?.Name),
                    BookingDate = booking.CreatedDate,
                    RoomId = booking.RoomId,
                    RoomName = booking.Room?.Name ?? "Không xác định",
                    CheckInDate = booking.CheckIn,
                    CheckOutDate = booking.CheckOut,
                    GuestsCount = booking.Guests,
                    GuestName = currentUser?.FullName ?? "Khách hàng",
                    TotalPrice = booking.TotalPrice,
                    IsCancellable = booking.CheckIn > DateTime.Now &&
                                   booking.BookingStatus?.Name != "Đã hủy"
                };

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

                // Kiểm tra có thể hủy không
                if (booking.CheckIn <= DateTime.Now || booking.BookingStatus?.Name == "Đã hủy")
                {
                    TempData["Error"] = "Không thể hủy đặt phòng này";
                    return RedirectToAction("Details", new { id });
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