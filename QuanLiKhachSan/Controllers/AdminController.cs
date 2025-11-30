
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLiKhachSan.Data;
using QuanLiKhachSan.Models;
using QuanLiKhachSan.ViewModels.Admin;
using System.Globalization;
using System.Security.Claims;


namespace QuanLiKhachSan.Controllers
{
    [Route("Admin")]
    public class AdminController : Controller
    {
            [HttpGet("Dashboard")]
            public async Task<IActionResult> Dashboard(DateTime? start = null, DateTime? end = null, string? status = null, int? roomId = null, string? sort = null, int page = 1, int pageSize = 10)
        {
            try
            {
                // Calculate metrics (async, safe)
                var totalRevenue = await _context.Bookings.SumAsync(b => (decimal?)b.TotalPrice) ?? 0m;
                var totalBookings = await _context.Bookings.CountAsync();
                var totalRooms = await _context.Rooms.CountAsync();
                var now = DateTime.Now;
                var occupiedRooms = await _context.Bookings.CountAsync(b => b.CheckIn <= now && b.CheckOut >= now);
                var occupancyRate = totalRooms > 0 ? (double)occupiedRooms / totalRooms * 100 : 0;
                var totalCustomers = await _context.Users.CountAsync();

                // normalize filter dates (use CreatedDate filter)
                DateTime startDate = start ?? DateTime.UtcNow.AddMonths(-1);
                DateTime endDate = (end ?? DateTime.UtcNow).Date.AddDays(1).AddTicks(-1);

                // Recent Bookings with filters, sorting and paging
                var bookingsQuery = _context.Bookings
                    .Include(b => b.User)
                    .Include(b => b.Room)
                    .Include(b => b.BookingStatus)
                    .AsQueryable();

                bookingsQuery = bookingsQuery.Where(b => b.CreatedDate >= startDate && b.CreatedDate <= endDate);
                if (!string.IsNullOrWhiteSpace(status))
                {
                    bookingsQuery = bookingsQuery.Where(b => b.BookingStatus != null && b.BookingStatus.Name == status);
                }
                if (roomId.HasValue)
                {
                    bookingsQuery = bookingsQuery.Where(b => b.RoomId == roomId.Value);
                }

                var totalRecent = await bookingsQuery.CountAsync();

                // sorting
                bookingsQuery = sort switch
                {
                    "price_asc" => bookingsQuery.OrderBy(b => b.TotalPrice),
                    "price_desc" => bookingsQuery.OrderByDescending(b => b.TotalPrice),
                    "checkin_asc" => bookingsQuery.OrderBy(b => b.CheckIn),
                    "checkin_desc" => bookingsQuery.OrderByDescending(b => b.CheckIn),
                    _ => bookingsQuery.OrderByDescending(b => b.CreatedDate),
                };

                var recentBookings = await bookingsQuery
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(b => new DashboardViewModel.RecentBookingDto
                    {
                        BookingNumber = $"BK{b.Id:D6}",
                        CustomerName = b.User != null ? b.User.FullName : "-",
                        RoomName = b.Room != null ? b.Room.Name : "-",
                        CheckIn = b.CheckIn,
                        CheckOut = b.CheckOut,
                        Status = b.BookingStatus != null ? b.BookingStatus.Name : "-",
                        TotalPrice = b.TotalPrice
                    })
                    .ToListAsync();

                // Recent Reviews (10) - lấy từ DB trước, xử lý Initials trên bộ nhớ
                var recentReviewsRaw = await _context.Reviews
                    .Include(r => r.User)
                    .Include(r => r.Room)
                    .OrderByDescending(r => r.CreatedDate)
                    .Take(10)
                    .Select(r => new {
                        CustomerName = r.User != null ? r.User.FullName : "-",
                        CreatedDate = r.CreatedDate,
                        Rating = r.Rating,
                        RoomName = r.Room != null ? r.Room.Name : "-",
                        Comment = r.Comment
                    })
                    .ToListAsync();

                var recentReviews = recentReviewsRaw.Select(r => new DashboardViewModel.RecentReviewDto
                {
                    CustomerName = r.CustomerName,
                    Initials = string.Join("", r.CustomerName.Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(x => x[0])).ToUpper(),
                    CreatedDate = r.CreatedDate,
                    Rating = r.Rating,
                    RoomName = r.RoomName,
                    Comment = r.Comment
                }).ToList();

                // Top Customers (10)
                var topCustomersRaw = await _context.Users
                    .Select(u => new {
                        u.FullName,
                        u.Email,
                        BookingCount = u.Bookings.Count(),
                        TotalRevenue = u.Bookings.Sum(b => (decimal?)b.TotalPrice) ?? 0m
                    })
                    .OrderByDescending(x => x.TotalRevenue)
                    .ThenByDescending(x => x.BookingCount)
                    .Take(10)
                    .ToListAsync();

                var topCustomers = topCustomersRaw.Select(x => new DashboardViewModel.TopCustomerDto
                {
                    CustomerName = x.FullName,
                    Initials = string.Join("", x.FullName.Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(n => n[0])).ToUpper(),
                    Email = x.Email,
                    BookingCount = x.BookingCount,
                    TotalRevenue = x.TotalRevenue
                }).ToList();

                // Room Status Counts
                var rooms = await _context.Rooms.ToListAsync();
                    var occupiedRoomIdsList = await _context.Bookings
                        .Where(b => b.CheckIn <= now && b.CheckOut >= now)
                        .Select(b => b.RoomId)
                        .ToListAsync();
                    var occupiedRoomIds = occupiedRoomIdsList.ToHashSet();
            var roomStatusCounts = new DashboardViewModel.RoomStatusCountsDto
            {
                Available = rooms.Count(r => r.IsAvailable && !occupiedRoomIds.Contains(r.Id)),
                Occupied = rooms.Count(r => occupiedRoomIds.Contains(r.Id)),
                Cleaning = 0, // Add logic if you track cleaning status
                Maintenance = rooms.Count(r => !r.IsAvailable)
            };

            // Notifications (10, sample logic)
            var notifications = new List<DashboardViewModel.NotificationDto>();
            // Booking notifications
            var latestBooking = recentBookings.FirstOrDefault();
            if (latestBooking != null)
            {
                notifications.Add(new DashboardViewModel.NotificationDto
                {
                    IconClass = "fas fa-calendar-check",
                    BgClass = "bg-primary",
                    Title = "Đặt phòng mới",
                    Text = $"{latestBooking.CustomerName} đã đặt {latestBooking.RoomName}",
                    TimeAgo = "Vừa xong",
                    Unread = true
                });
            }
            // Review notifications
            var latestReview = recentReviews.FirstOrDefault();
            if (latestReview != null)
            {
                notifications.Add(new DashboardViewModel.NotificationDto
                {
                    IconClass = "fas fa-star",
                    BgClass = "bg-warning",
                    Title = "Đánh giá mới",
                    Text = $"{latestReview.CustomerName} đã đánh giá {latestReview.Rating} sao cho {latestReview.RoomName}",
                    TimeAgo = "1 giờ trước",
                    Unread = true
                });
            }
            // Payment notifications (sample)
            var latestPayment = _context.Payments
                .OrderByDescending(p => p.PaymentDate)
                .Include(p => p.Booking)
                .FirstOrDefault();
            if (latestPayment != null)
            {
                notifications.Add(new DashboardViewModel.NotificationDto
                {
                    IconClass = "fas fa-check-circle",
                    BgClass = "bg-success",
                    Title = "Thanh toán thành công",
                    Text = $"Thanh toán #{latestPayment.Id} đã được xác nhận",
                    TimeAgo = "2 giờ trước",
                    Unread = false
                });
            }
            // User notifications (sample)
            var latestUser = _context.Users
                .OrderByDescending(u => u.Id)
                .FirstOrDefault();
            if (latestUser != null)
            {
                notifications.Add(new DashboardViewModel.NotificationDto
                {
                    IconClass = "fas fa-user-plus",
                    BgClass = "bg-info",
                    Title = "Người dùng mới",
                    Text = $"{latestUser.FullName} đã đăng ký tài khoản",
                    TimeAgo = "3 giờ trước",
                    Unread = false
                });
            }

            var dashboardVM = new DashboardViewModel
            {
                TotalRevenue = totalRevenue,
                TotalBookings = totalBookings,
                RoomOccupancyRate = occupancyRate,
                TotalCustomers = totalCustomers,
                RecentBookings = recentBookings,
                RecentReviews = recentReviews,
                TopCustomers = topCustomers,
                RoomStatusCounts = roomStatusCounts,
                Notifications = notifications
            };

                // Additional charts: revenue by month (last 12 months)
                var twelveMonthsAgo = DateTime.UtcNow.AddMonths(-11);
                var revenueByMonth = await _context.Bookings
                    .Where(b => b.CreatedDate >= new DateTime(twelveMonthsAgo.Year, twelveMonthsAgo.Month, 1))
                    .GroupBy(b => new { b.CreatedDate.Year, b.CreatedDate.Month })
                    .Select(g => new { Year = g.Key.Year, Month = g.Key.Month, Total = g.Sum(x => (decimal?)x.TotalPrice) ?? 0m })
                    .ToListAsync();

                var revenuePoints = new List<DashboardViewModel.RevenuePoint>();
                for (int i = 11; i >= 0; i--)
                {
                    var dt = DateTime.UtcNow.AddMonths(-i);
                    var found = revenueByMonth.FirstOrDefault(r => r.Year == dt.Year && r.Month == dt.Month);
                    revenuePoints.Add(new DashboardViewModel.RevenuePoint { Label = dt.ToString("MMM yyyy"), Value = found?.Total ?? 0m });
                }

                dashboardVM.RevenueByMonth = revenuePoints;

                // bookings by status
                var bookingsByStatus = await _context.Bookings
                    .Include(b => b.BookingStatus)
                    .GroupBy(b => b.BookingStatus != null ? b.BookingStatus.Name : "Unknown")
                    .Select(g => new { Status = g.Key, Count = g.Count() })
                    .ToListAsync();
                dashboardVM.BookingsByStatus = bookingsByStatus.Select(x => new KeyValuePair<string,int>(x.Status, x.Count)).ToList();

                // top rooms
                var topRooms = await _context.Bookings
                    .Where(b => b.RoomId != 0)
                    .GroupBy(b => new { b.RoomId, b.Room.Name })
                    .Select(g => new { RoomId = g.Key.RoomId, RoomName = g.Key.Name, Count = g.Count(), Revenue = g.Sum(x => (decimal?)x.TotalPrice) ?? 0m })
                    .OrderByDescending(x => x.Revenue)
                    .Take(10)
                    .ToListAsync();
                dashboardVM.TopRooms = topRooms.Select(x => new DashboardViewModel.TopRoomDto { RoomId = x.RoomId, RoomName = x.RoomName, BookingCount = x.Count, Revenue = x.Revenue }).ToList();

                // attach filters/paging metadata
                dashboardVM.FilterStart = startDate;
                dashboardVM.FilterEnd = endDate;
                dashboardVM.FilterStatus = status;
                dashboardVM.FilterRoomId = roomId;
                dashboardVM.Page = page;
                dashboardVM.PageSize = pageSize;
                dashboardVM.TotalRecentBookings = totalRecent;

                return View("Dashboard", dashboardVM);
            }
            catch (Exception ex)
            {
                // In Development: return 500 with message to help debugging
                return StatusCode(500, $"Dashboard error: {ex.Message}");
            }
        }

        private readonly ApplicationDbContext _context;

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("Bookings")]
        public async Task<IActionResult> Bookings()
        {
            // Lấy dữ liệu thực từ database
            var bookings = await _context.Bookings
                .Include(b => b.User)
                .Include(b => b.Room)
                .Include(b => b.BookingStatus)
                .ToListAsync();

            var viewModel = new BookingsViewModel
            {
                Bookings = bookings.Select(b => new BookingItemViewModel
                {
                    Id = b.Id,
                    BookingNumber = $"BK{b.Id:D6}",
                    CustomerName = b.User.FullName,
                    CustomerEmail = b.User.Email, // THÊM EMAIL
                    RoomName = b.Room.Name,
                    CheckInDate = b.CheckIn,
                    CheckOutDate = b.CheckOut,
                    GuestCount = b.Guests, // SỬA THÀNH GuestCount
                    TotalPrice = b.TotalPrice,
                    Status = b.BookingStatus.Name,
                    PaymentStatus = "Chưa thanh toán", // TẠM THỜI
                    BookingDate = b.CreatedDate
                }).ToList()
            };

            return View("AdminBookings", viewModel);
        }

        [HttpGet("Rooms")]
        public IActionResult Rooms()
        {
            var rooms = _context.Rooms.ToList();
            var now = DateTime.Now;
            var occupiedRoomIds = _context.Bookings
                .Where(b => b.CheckIn <= now && b.CheckOut >= now)
                .Select(b => b.RoomId)
                .ToHashSet();

            var viewModel = new RoomsViewModel
            {
                Rooms = rooms.Select(r => new RoomItemViewModel
                {
                    Id = r.Id,
                    Name = r.Name,
                    RoomType = r.RoomType,
                    PricePerNight = r.PricePerNight,
                    Capacity = r.Capacity,
                    Status = occupiedRoomIds.Contains(r.Id) ? "Đang sử dụng" : (r.IsAvailable ? "Trống" : "Bảo trì"),
                    Floor = 0, // Add if you have this info
                    RoomNumber = r.Id.ToString(), // Add if you have this info
                    Rating = r.AverageRating,
                    ImageUrl = r.ImageUrl,
                    Description = r.Description,
                    Amenities = new List<string>() // Add if you have this info
                }).ToList(),
                TotalRooms = rooms.Count,
                AvailableCount = rooms.Count(r => r.IsAvailable && !occupiedRoomIds.Contains(r.Id)),
                OccupiedCount = rooms.Count(r => occupiedRoomIds.Contains(r.Id)),
                MaintenanceCount = rooms.Count(r => !r.IsAvailable),
                CleaningCount = 0 // Add logic if you track cleaning status
            };
            return View("AdminRooms", viewModel);
        }

        [HttpGet("Users")]
        public async Task<IActionResult> Users(string? search = null, string? status = null, int page = 1)
        {
            // Query users from database
            var query = _context.Users
                .Include(u => u.Bookings)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(u => u.FullName.Contains(search) || u.Email.Contains(search));
            }

            // TODO: filter by status if you have a status field

            var totalUsers = await query.CountAsync();
            var pageSize = 10;

            var users = await query
                .OrderByDescending(u => u.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var vm = new UsersViewModel
            {
                TotalUsers = totalUsers,
                Page = page,
                PageSize = pageSize,
                Users = users.Select((u, idx) => new UserItemViewModel
                {
                    Id = u.Id,
                    UserId = $"UID{(page-1)*pageSize + idx + 1:000}",
                    FirstName = SplitFirstName(u.FullName),
                    LastName = SplitLastName(u.FullName),
                    Email = u.Email,
                    Phone = u.PhoneNumber ?? "",
                    RegisterDate = DateTime.Now, // no CreatedDate on IdentityUser by default
                    BookingsCount = u.Bookings?.Count() ?? 0,
                    TotalSpending = u.Bookings?.Sum(b => b.TotalPrice) ?? 0m,
                    Status = "active", // adapt if you store status
                    UserType = "normal",
                    Address = "",
                    LoyaltyPoints = 0,
                    LastLoginDate = null
                }).ToList()
            };

            // populate counts
            vm.ActiveCount = await _context.Users.CountAsync();
            vm.NewCount = await _context.Users.CountAsync();
            vm.VipCount = 0;
            vm.InactiveCount = 0;

            return View("AdminUsers", vm);
        }

        private static string SplitFirstName(string fullName)
        {
            if (string.IsNullOrWhiteSpace(fullName)) return "";
            var parts = fullName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return parts.Length > 0 ? parts[0] : fullName;
        }

        private static string SplitLastName(string fullName)
        {
            if (string.IsNullOrWhiteSpace(fullName)) return "";
            var parts = fullName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return parts.Length > 1 ? parts[parts.Length - 1] : "";
        }

        private List<UserItemViewModel> GetSampleUsers()
        {

            return new List<UserItemViewModel>();
        }


        [HttpGet("Promotions")]
        public IActionResult Promotions()
        {
            return View("AdminPromotions");
        }

        [HttpGet("Reports")]
        public IActionResult Reports()
        {
            return View("AdminReports");
        }

        [HttpGet("AddRoom")]
        public IActionResult AddRoom()
        {
            return View();
        }

        [HttpPost("AddRoom")]
        public async Task<IActionResult> AddRoom(RoomCreateViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Xử lý upload hình ảnh (giả lập)
                    string mainImagePath = "/images/rooms/room-placeholder.jpg";
                    if (model.MainImage != null)
                    {
                        // Giả lập đường dẫn ảnh thành công
                        mainImagePath = $"/images/rooms/{Guid.NewGuid()}.jpg";
                    }

                    // Giả lập thành công và chuyển hướng
                    TempData["SuccessMessage"] = "Thêm phòng mới thành công!";
                    return RedirectToAction("Rooms");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Có lỗi khi thêm phòng: {ex.Message}");
                }
            }
            return View(model);
        }

        public IActionResult Reviews()
        {
            // Tạo dữ liệu mẫu trực tiếp
            var reviews = new List<ReviewViewModel>
            {
                new ReviewViewModel
                {
                    Id = 1,
                    CustomerName = "Nguyễn Văn A",
                    CustomerEmail = "nguyena@email.com",
                    RoomName = "Deluxe Room",
                    RoomType = "Deluxe",
                    Rating = 5,
                    Title = "Trải nghiệm tuyệt vời!",
                    Comment = "Phòng rất sạch sẽ, nhân viên thân thiện. Tôi rất hài lòng!",
                    Status = "Approved",
                    CreatedDate = DateTime.Now.AddDays(-2)
                },
                new ReviewViewModel
                {
                    Id = 2,
                    CustomerName = "Trần Thị B",
                    CustomerEmail = "tranb@email.com",
                    RoomName = "Superior Room",
                    RoomType = "Superior",
                    Rating = 4,
                    Title = "Tốt nhưng cần cải thiện",
                    Comment = "Phòng đẹp nhưng wifi hơi chậm. Nhìn chung là tốt.",
                    Status = "Pending",
                    CreatedDate = DateTime.Now.AddDays(-1)
                },
                new ReviewViewModel
                {
                    Id = 3,
                    CustomerName = "Lê Văn C",
                    CustomerEmail = "lec@email.com",
                    RoomName = "Executive Suite",
                    RoomType = "Suite",
                    Rating = 3,
                    Title = "Bình thường",
                    Comment = "Phòng ổn nhưng không có gì đặc biệt. Giá hơi cao.",
                    Status = "Pending",
                    CreatedDate = DateTime.Now
                },
                new ReviewViewModel
                {
                    Id = 4,
                    CustomerName = "Phạm Thị D",
                    CustomerEmail = "phamd@email.com",
                    RoomName = "Family Room",
                    RoomType = "Family",
                    Rating = 5,
                    Title = "Hoàn hảo cho gia đình",
                    Comment = "Phòng rộng rãi, view đẹp. Dịch vụ rất tốt!",
                    Status = "Approved",
                    CreatedDate = DateTime.Now.AddDays(-3)
                },
                new ReviewViewModel
                {
                    Id = 5,
                    CustomerName = "Hoàng Văn E",
                    CustomerEmail = "hoange@email.com",
                    RoomName = "Standard Room",
                    RoomType = "Standard",
                    Rating = 2,
                    Title = "Không như mong đợi",
                    Comment = "Phòng nhỏ, thiết bị cũ. Cần cải thiện nhiều.",
                    Status = "Rejected",
                    CreatedDate = DateTime.Now.AddDays(-5)
                }
            };

            return View(reviews);
        }

        [HttpPost("ConfirmBooking/{id}")]
        public async Task<IActionResult> ConfirmBooking(int id)
        {
            try
            {
                var booking = await _context.Bookings
                    .Include(b => b.BookingStatus)
                    .FirstOrDefaultAsync(b => b.Id == id);

                if (booking == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy đơn đặt phòng" });
                }

                var confirmedStatus = await _context.BookingStatuses
                    .FirstOrDefaultAsync(s => s.Name == "Đã xác nhận");

                if (confirmedStatus == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy trạng thái xác nhận" });
                }

                var previousStatusId = booking.BookingStatusId;
                booking.BookingStatusId = confirmedStatus.Id;

                var history = new BookingStatusHistory
                {
                    BookingId = booking.Id,
                    FromBookingStatusId = previousStatusId,
                    ToBookingStatusId = confirmedStatus.Id,
                    ChangedByUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                    Note = "Xác nhận bởi quản trị viên",
                    ChangedAt = DateTime.UtcNow
                };

                _context.BookingStatusHistories.Add(history);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Đã xác nhận đơn đặt phòng thành công", bookingId = booking.Id, newStatus = confirmedStatus.Name });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Lỗi: {ex.Message}" });
            }
        }

        [HttpPost("CancelBooking/{id}")]
        public async Task<IActionResult> CancelBooking(int id)
        {
            try
            {
                var booking = await _context.Bookings
                    .Include(b => b.BookingStatus)
                    .FirstOrDefaultAsync(b => b.Id == id);

                if (booking == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy đơn đặt phòng" });
                }

                var cancelledStatus = await _context.BookingStatuses
                    .FirstOrDefaultAsync(s => s.Name == "Đã hủy");

                if (cancelledStatus == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy trạng thái hủy" });
                }

                var previousStatusId = booking.BookingStatusId;
                booking.BookingStatusId = cancelledStatus.Id;

                var history = new BookingStatusHistory
                {
                    BookingId = booking.Id,
                    FromBookingStatusId = previousStatusId,
                    ToBookingStatusId = cancelledStatus.Id,
                    ChangedByUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                    Note = "Hủy bởi quản trị viên",
                    ChangedAt = DateTime.UtcNow
                };

                _context.BookingStatusHistories.Add(history);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Đã hủy đơn đặt phòng thành công", bookingId = booking.Id, newStatus = cancelledStatus.Name });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Lỗi: {ex.Message}" });
            }
        }

        [HttpPost("BulkConfirm")]
        public async Task<IActionResult> BulkConfirm([FromBody] List<int> ids)
        {
            if (ids == null || !ids.Any()) return Json(new { success = false, message = "Không có đơn nào được chọn" });
            using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                var confirmedStatus = await _context.BookingStatuses.FirstOrDefaultAsync(s => s.Name == "Đã xác nhận");
                if (confirmedStatus == null) return Json(new { success = false, message = "Không tìm thấy trạng thái xác nhận" });

                var bookings = await _context.Bookings.Where(b => ids.Contains(b.Id)).ToListAsync();
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                var updatedIds = new List<int>();
                foreach (var booking in bookings)
                {
                    var previous = booking.BookingStatusId;
                    booking.BookingStatusId = confirmedStatus.Id;

                    _context.BookingStatusHistories.Add(new BookingStatusHistory
                    {
                        BookingId = booking.Id,
                        FromBookingStatusId = previous,
                        ToBookingStatusId = confirmedStatus.Id,
                        ChangedByUserId = userId,
                        Note = "Xác nhận hàng loạt bởi quản trị viên",
                        ChangedAt = DateTime.UtcNow
                    });

                    updatedIds.Add(booking.Id);
                }

                await _context.SaveChangesAsync();
                await tx.CommitAsync();
                return Json(new { success = true, message = $"Đã xác nhận {updatedIds.Count} đơn", updatedIds, newStatus = confirmedStatus.Name });
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost("BulkCancel")]
        public async Task<IActionResult> BulkCancel([FromBody] List<int> ids)
        {
            if (ids == null || !ids.Any()) return Json(new { success = false, message = "Không có đơn nào được chọn" });

            using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                var cancelledStatus = await _context.BookingStatuses.FirstOrDefaultAsync(s => s.Name == "Đã hủy");
                if (cancelledStatus == null) return Json(new { success = false, message = "Không tìm thấy trạng thái hủy" });

                var bookings = await _context.Bookings.Where(b => ids.Contains(b.Id)).ToListAsync();
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var updatedIds = new List<int>();
                foreach (var booking in bookings)
                {
                    var previous = booking.BookingStatusId;
                    booking.BookingStatusId = cancelledStatus.Id;

                    _context.BookingStatusHistories.Add(new BookingStatusHistory
                    {
                        BookingId = booking.Id,
                        FromBookingStatusId = previous,
                        ToBookingStatusId = cancelledStatus.Id,
                        ChangedByUserId = userId,
                        Note = "Hủy hàng loạt bởi quản trị viên",
                        ChangedAt = DateTime.UtcNow
                    });

                    updatedIds.Add(booking.Id);
                }

                await _context.SaveChangesAsync();
                await tx.CommitAsync();
                return Json(new { success = true, message = $"Đã hủy {updatedIds.Count} đơn", updatedIds, newStatus = cancelledStatus.Name });
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet("Bookings/Details/{id}")]
        public async Task<IActionResult> BookingDetails(int id)
        {
            var booking = await _context.Bookings
                .Include(b => b.User)
                .Include(b => b.Room)
                .Include(b => b.BookingStatus)
                .Include(b => b.Payments)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (booking == null)
            {
                return NotFound();
            }

            var histories = await _context.BookingStatusHistories
                .Where(h => h.BookingId == id)
                .Include(h => h.FromBookingStatus)
                .Include(h => h.ToBookingStatus)
                .OrderByDescending(h => h.ChangedAt)
                .ToListAsync();

            var vm = new QuanLiKhachSan.ViewModels.Booking.BookingDetailsViewModel
            {
                Id = booking.Id,
                BookingNumber = $"BK{booking.Id:D6}",
                Status = booking.BookingStatus?.Name ?? "-",
                BookingDate = booking.CreatedDate,
                RoomId = booking.RoomId,
                RoomName = booking.Room?.Name ?? "-",
                RoomType = booking.Room?.RoomType ?? "",
                RoomDescription = booking.Room?.Description ?? "",
                RoomImageUrl = booking.Room?.ImageUrl ?? "/images/rooms/room-placeholder.jpg",
                RoomRating = booking.Room?.AverageRating ?? 0,
                RoomCapacity = booking.Room?.Capacity ?? 1,
                CheckInDate = booking.CheckIn,
                CheckOutDate = booking.CheckOut,
                GuestsCount = booking.Guests,
                GuestName = booking.User?.FullName ?? booking.UserId,
                GuestEmail = booking.User?.Email ?? "",
                GuestPhone = booking.User?.PhoneNumber ?? "",
                RoomPrice = booking.TotalPrice,
                TotalPrice = booking.TotalPrice,
                PaymentStatus = booking.Payments.Any() ? "Đã thanh toán" : "Chưa thanh toán",
                PaymentMethod = booking.Payments.FirstOrDefault()?.PaymentStatus.Name ?? "",
                BookingActivities = new List<QuanLiKhachSan.ViewModels.Booking.BookingActivityViewModel>()
            };

            // Add status history activities
            foreach (var h in histories)
            {
                var type = "update";
                var toName = h.ToBookingStatus?.Name ?? "";
                if (toName.Contains("xác nhận") || toName.Contains("Đã xác nhận")) type = "confirm";
                if (toName.Contains("hủy") || toName.Contains("Đã hủy")) type = "cancel";

                vm.BookingActivities.Add(new QuanLiKhachSan.ViewModels.Booking.BookingActivityViewModel
                {
                    Date = h.ChangedAt,
                    Title = h.ToBookingStatus?.Name ?? "Trạng thái cập nhật",
                    Description = h.Note ?? $"{h.FromBookingStatus?.Name ?? "-"} → {h.ToBookingStatus?.Name ?? "-"}",
                    Type = type
                });
            }

            // Add payments as activities
            foreach (var p in booking.Payments.OrderByDescending(p => p.PaymentDate))
            {
                vm.BookingActivities.Add(new QuanLiKhachSan.ViewModels.Booking.BookingActivityViewModel
                {
                    Date = p.PaymentDate,
                    Title = $"Thanh toán: {p.Amount:N0} VNĐ",
                    Description = p.PaymentStatus?.Name ?? "",
                    Type = "payment"
                });
            }

            return View("Admin/Bookings/Details", vm);
        }

    }
}