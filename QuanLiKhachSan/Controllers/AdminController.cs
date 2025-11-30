
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

            // Map bookings to view model items
            var items = bookings.Select(b => new BookingItemViewModel
            {
                Id = b.Id,
                BookingNumber = $"BK{b.Id:D6}",
                CustomerName = b.User?.FullName ?? "-",
                CustomerEmail = b.User?.Email ?? "",
                RoomName = b.Room?.Name ?? "-",
                CheckInDate = b.CheckIn,
                CheckOutDate = b.CheckOut,
                GuestCount = b.Guests,
                TotalPrice = b.TotalPrice,
                Status = b.BookingStatus?.Name ?? "",
                PaymentStatus = b.Payments != null && b.Payments.Any() ? "Đã thanh toán" : "Chưa thanh toán",
                BookingDate = b.CreatedDate
            }).ToList();

            // Compute counts by status using status name keywords (robust to language)
            int pending = bookings.Count(b => (b.BookingStatus == null) || (b.BookingStatus.Name != null && (b.BookingStatus.Name.ToLower().Contains("chờ") || b.BookingStatus.Name.ToLower().Contains("pending") || b.BookingStatus.Name.ToLower().Contains("wait"))));
            int confirmed = bookings.Count(b => b.BookingStatus != null && (b.BookingStatus.Name.ToLower().Contains("xác nhận") || b.BookingStatus.Name.ToLower().Contains("confirmed") || b.BookingStatus.Name.ToLower().Contains("confirm")));
            int completed = bookings.Count(b => b.BookingStatus != null && (b.BookingStatus.Name.ToLower().Contains("hoàn thành") || b.BookingStatus.Name.ToLower().Contains("completed") || b.BookingStatus.Name.ToLower().Contains("complete")));
            int cancelled = bookings.Count(b => b.BookingStatus != null && (b.BookingStatus.Name.ToLower().Contains("hủy") || b.BookingStatus.Name.ToLower().Contains("cancelled") || b.BookingStatus.Name.ToLower().Contains("cancel")));

            var viewModel = new BookingsViewModel
            {
                Bookings = items,
                TotalBookings = items.Count,
                PendingCount = pending,
                ConfirmedCount = confirmed,
                CompletedCount = completed,
                CancelledCount = cancelled
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

        [HttpGet("Users/Details/{id}")]
        public async Task<IActionResult> UserDetails(string id)
        {
            var user = await _context.Users
                .Include(u => u.Bookings)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null) return NotFound();

            var detail = new UserDetailViewModel
            {
                Id = user.Id,
                UserId = user.Id,
                FirstName = SplitFirstName(user.FullName),
                LastName = SplitLastName(user.FullName),
                Email = user.Email,
                Phone = user.PhoneNumber ?? string.Empty,
                RegisterDate = DateTime.Now, // replace with actual created date if available
                BookingsCount = user.Bookings?.Count() ?? 0,
                TotalSpending = user.Bookings?.Sum(b => b.TotalPrice) ?? 0m,
                Status = "active",
                UserType = "normal",
                Address = string.Empty,
                LoyaltyPoints = 0,
                LastLoginDate = null
            };

            // recent bookings
            detail.RecentBookings = user.Bookings
                .OrderByDescending(b => b.CreatedDate)
                .Take(5)
                .Select(b => new UserBookingViewModel
                {
                    BookingNumber = $"BK{b.Id:D6}",
                    RoomName = b.Room?.Name ?? "-",
                    CheckIn = b.CheckIn,
                    CheckOut = b.CheckOut,
                    Status = b.BookingStatus?.Name ?? "-",
                    TotalPrice = b.TotalPrice
                }).ToList();

            // recent activities (simple sample)
            detail.RecentActivities = new List<UserActivityViewModel>
            {
                new UserActivityViewModel { Date = DateTime.Now.AddDays(-1), Title = "Đăng nhập", Description = "Đăng nhập thành công", Type = "login" }
            };

            return PartialView("~/Views/Admin/Users/Details.cshtml", detail);
        }

        // --- Reviews moderation endpoints (AJAX) ---
        [HttpPost("Reviews/Approve/{id}")]
        public async Task<IActionResult> ApproveReview(int id)
        {
            var review = await _context.Reviews.FirstOrDefaultAsync(r => r.Id == id);
            if (review == null) return Json(new { success = false, message = "Không tìm thấy đánh giá" });
            try
            {
                review.Status = "Active"; // approved
                review.UpdatedDate = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = $"Đã phê duyệt đánh giá #{id}" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost("Reviews/Reject/{id}")]
        public async Task<IActionResult> RejectReview(int id)
        {
            var review = await _context.Reviews.FirstOrDefaultAsync(r => r.Id == id);
            if (review == null) return Json(new { success = false, message = "Không tìm thấy đánh giá" });
            try
            {
                review.Status = "Inactive"; // rejected
                review.UpdatedDate = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = $"Đã từ chối đánh giá #{id}" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // --- Promotions (in-memory demo store) ---
        private static List<dynamic> _promotionsStore = new List<dynamic>();

        [HttpGet("Promotions/List")]
        public IActionResult PromotionsList()
        {
            // Return a partial with current promotions
            return PartialView("~/Views/Admin/Promotions/_List.cshtml", _promotionsStore);
        }

        [HttpPost("Promotions/Create")]
        public IActionResult CreatePromotion([FromForm] string title, [FromForm] string code, [FromForm] decimal discount)
        {
            var id = (_promotionsStore.LastOrDefault()?.Id as int? ?? 0) + 1;
            _promotionsStore.Add(new { Id = id, Title = title, Code = code, Discount = discount, Active = true, Created = DateTime.Now });
            return Json(new { success = true, message = "Tạo khuyến mãi thành công" });
        }

        [HttpPost("Promotions/Delete/{id}")]
        public IActionResult DeletePromotion(int id)
        {
            var item = _promotionsStore.FirstOrDefault(p => (int)p.Id == id);
            if (item != null) _promotionsStore.Remove(item);
            return Json(new { success = true });
        }

        // --- Reports data endpoint ---
        [HttpGet("Reports/Data")]
        public async Task<IActionResult> ReportsData()
        {
            // Build simple demo metrics using real data where available
            var totalRevenue = await _context.Bookings.SumAsync(b => (decimal?)b.TotalPrice) ?? 0m;
            var totalBookings = await _context.Bookings.CountAsync();
            var totalCustomers = await _context.Users.CountAsync();

            // revenue last 12 months
            var twelveMonthsAgo = DateTime.UtcNow.AddMonths(-11);
            var revenueByMonth = await _context.Bookings
                .Where(b => b.CreatedDate >= new DateTime(twelveMonthsAgo.Year, twelveMonthsAgo.Month, 1))
                .GroupBy(b => new { b.CreatedDate.Year, b.CreatedDate.Month })
                .Select(g => new { Year = g.Key.Year, Month = g.Key.Month, Total = g.Sum(x => (decimal?)x.TotalPrice) ?? 0m })
                .ToListAsync();

            var labels = new List<string>();
            var values = new List<decimal>();
            for (int i = 11; i >= 0; i--)
            {
                var dt = DateTime.UtcNow.AddMonths(-i);
                labels.Add(dt.ToString("MMM yyyy"));
                var found = revenueByMonth.FirstOrDefault(r => r.Year == dt.Year && r.Month == dt.Month);
                values.Add(found?.Total ?? 0m);
            }

            // Room counts and occupancy
            var rooms = await _context.Rooms.ToListAsync();
            var now = DateTime.Now;
            var occupiedRoomIds = await _context.Bookings
                .Where(b => b.CheckIn <= now && b.CheckOut >= now)
                .Select(b => b.RoomId)
                .Distinct()
                .ToListAsync();

            var totalRooms = rooms.Count;
            var occupiedCount = occupiedRoomIds.Distinct().Count();
            var availableCount = rooms.Count(r => r.IsAvailable && !occupiedRoomIds.Contains(r.Id));
            var maintenanceCount = rooms.Count(r => !r.IsAvailable);

            // bookings by status
            var bookingsByStatusRaw = await _context.Bookings
                .Include(b => b.BookingStatus)
                .GroupBy(b => b.BookingStatus != null ? b.BookingStatus.Name : "Unknown")
                .Select(g => new { Status = g.Key ?? "Unknown", Count = g.Count() })
                .ToListAsync();

            var bookingsStatusLabels = bookingsByStatusRaw.Select(x => x.Status).ToList();
            var bookingsStatusValues = bookingsByStatusRaw.Select(x => x.Count).ToList();

            // top customers by revenue (top 10)
            var topCustomersRaw = await _context.Bookings
                .Where(b => b.UserId != null)
                .GroupBy(b => b.UserId)
                .Select(g => new { UserId = g.Key, Total = g.Sum(x => (decimal?)x.TotalPrice) ?? 0m, Count = g.Count() })
                .OrderByDescending(x => x.Total)
                .Take(10)
                .ToListAsync();

            var userIds = topCustomersRaw.Select(x => x.UserId).ToList();
            var users = await _context.Users.Where(u => userIds.Contains(u.Id)).ToListAsync();

            var topCustomers = topCustomersRaw.Select(x => new { UserId = x.UserId, Name = users.FirstOrDefault(u => u.Id == x.UserId)?.FullName ?? x.UserId, Total = x.Total, Count = x.Count }).ToList();

            return Json(new
            {
                totalRevenue,
                totalBookings,
                totalCustomers,
                labels,
                values,
                roomCounts = new { totalRooms, occupiedCount, availableCount, maintenanceCount },
                bookingsByStatus = new { labels = bookingsStatusLabels, values = bookingsStatusValues },
                topCustomers
            });
        }

        // --- Settings save/load (JSON file under App_Data) ---
        private string GetSettingsPath() => Path.Combine(Directory.GetCurrentDirectory(), "App_Data", "admin-settings.json");

        [HttpGet("Settings/Load")]
        public IActionResult LoadSettings()
        {
            try
            {
                var path = GetSettingsPath();
                if (!System.IO.File.Exists(path)) return Json(new { success = true, settings = new { SiteTitle = "Luxury Hotel" } });
                var json = System.IO.File.ReadAllText(path);
                var obj = System.Text.Json.JsonSerializer.Deserialize<object>(json);
                return Json(new { success = true, settings = obj });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost("Settings/Save")]
        public IActionResult SaveSettings([FromForm] string siteTitle, [FromForm] string supportEmail)
        {
            try
            {
                var path = GetSettingsPath();
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                var obj = new { SiteTitle = siteTitle, SupportEmail = supportEmail };
                var json = System.Text.Json.JsonSerializer.Serialize(obj, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                System.IO.File.WriteAllText(path, json);
                return Json(new { success = true, message = "Lưu cài đặt thành công" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost("Users/ToggleLock/{id}")]
        public async Task<IActionResult> ToggleLock(string id)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
            if (user == null) return Json(new { success = false, message = "Không tìm thấy người dùng" });

            try
            {
                if (user.LockoutEnd == null || user.LockoutEnd <= DateTimeOffset.UtcNow)
                {
                    user.LockoutEnd = DateTimeOffset.UtcNow.AddYears(100);
                }
                else
                {
                    user.LockoutEnd = null;
                }

                await _context.SaveChangesAsync();
                return Json(new { success = true, locked = user.LockoutEnd != null });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        public class BulkUsersActionDto
        {
            public string Action { get; set; }
            public List<string> Ids { get; set; } = new List<string>();
        }

        [HttpPost("Users/BulkAction")]
        public async Task<IActionResult> BulkAction([FromBody] BulkUsersActionDto model)
        {
            if (model == null || model.Ids == null || !model.Ids.Any()) return Json(new { success = false, message = "Không có người dùng nào được chọn" });

            using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                var users = await _context.Users.Where(u => model.Ids.Contains(u.Id)).ToListAsync();

                foreach (var u in users)
                {
                    switch (model.Action?.ToLower())
                    {
                        case "activate":
                            u.LockoutEnd = null; break;
                        case "deactivate":
                            u.LockoutEnd = DateTimeOffset.UtcNow.AddYears(100); break;
                        case "verify":
                            u.EmailConfirmed = true; break;
                    }
                }

                await _context.SaveChangesAsync();
                await tx.CommitAsync();
                return Json(new { success = true, message = $"Thực hiện {model.Action} cho {users.Count} tài khoản" });
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                return Json(new { success = false, message = ex.Message });
            }
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

        [HttpGet("Reports/Revenue")]
        public IActionResult ReportsRevenue()
        {
            return View("~/Views/Admin/Reports/Revenue.cshtml");
        }

        [HttpGet("Reports/Occupancy")]
        public IActionResult ReportsOccupancy()
        {
            return View("~/Views/Admin/Reports/Occupancy.cshtml");
        }

        [HttpGet("Reports/Customers")]
        public IActionResult ReportsCustomers()
        {
            return View("~/Views/Admin/Reports/Customers.cshtml");
        }

        [HttpGet("Settings")]
        public IActionResult Settings()
        {
            return View("Settings");
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

        public async Task<IActionResult> Reviews()
        {
            var reviewsRaw = await _context.Reviews
                .Include(r => r.User)
                .Include(r => r.Room)
                .OrderByDescending(r => r.CreatedDate)
                .ToListAsync();

            var reviews = reviewsRaw.Select(r => new ReviewViewModel
            {
                Id = r.Id,
                CustomerName = r.User?.FullName ?? "-",
                CustomerEmail = r.User?.Email ?? "",
                RoomName = r.Room?.Name ?? "-",
                RoomType = r.Room?.RoomType ?? "",
                Rating = r.Rating,
                Title = "", // no title in model
                Comment = r.Comment,
                Status = (r.Status ?? "").Equals("Active", StringComparison.OrdinalIgnoreCase) ? "Approved" : ((r.Status ?? "").Equals("Inactive", StringComparison.OrdinalIgnoreCase) ? "Rejected" : "Pending"),
                CreatedDate = r.CreatedDate
            }).ToList();

            return View(reviews);
        }

        [HttpGet("Reviews/Moderate")]
        public async Task<IActionResult> ModerateReviews()
        {
            var reviewsRaw = await _context.Reviews
                .Include(r => r.User)
                .Include(r => r.Room)
                .OrderByDescending(r => r.CreatedDate)
                .ToListAsync();

            var reviews = reviewsRaw.Select(r => new ReviewViewModel
            {
                Id = r.Id,
                CustomerName = r.User?.FullName ?? "-",
                CustomerEmail = r.User?.Email ?? "",
                RoomName = r.Room?.Name ?? "-",
                RoomType = r.Room?.RoomType ?? "",
                Rating = r.Rating,
                Title = "",
                Comment = r.Comment,
                Status = (r.Status ?? "").Equals("Active", StringComparison.OrdinalIgnoreCase) ? "Approved" : ((r.Status ?? "").Equals("Inactive", StringComparison.OrdinalIgnoreCase) ? "Rejected" : "Pending"),
                CreatedDate = r.CreatedDate
            }).ToList();

            return View("~/Views/Admin/Reviews/Moderate.cshtml", reviews);
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
                PaymentStatus = (booking.Payments != null && booking.Payments.Any()) ? "Đã thanh toán" : "Chưa thanh toán",
                PaymentMethod = booking.Payments?.FirstOrDefault()?.PaymentStatus?.Name ?? "",
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
            var payments = booking.Payments?.OrderByDescending(p => p.PaymentDate) ?? Enumerable.Empty<QuanLiKhachSan.Models.Payment>();
            foreach (var p in payments)
            {
                vm.BookingActivities.Add(new QuanLiKhachSan.ViewModels.Booking.BookingActivityViewModel
                {
                    Date = p.PaymentDate,
                    Title = $"Thanh toán: {p.Amount:N0} VNĐ",
                    Description = p.PaymentStatus?.Name ?? "",
                    Type = "payment"
                });
            }

            return View("~/Views/Admin/Bookings/Details.cshtml", vm);
        }

    }
}