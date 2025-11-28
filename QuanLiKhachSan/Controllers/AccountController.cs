using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLiKhachSan.Data;
using QuanLiKhachSan.Models;
using QuanLiKhachSan.ViewModels.Account;

namespace QuanLiKhachSan.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ILogger<AccountController> _logger;
        private readonly ApplicationDbContext _context;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ILogger<AccountController> logger,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
            _context = context;
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            var model = new LoginViewModel
            {
                ReturnUrl = returnUrl ?? Url.Content("~/")
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            model.ReturnUrl ??= Url.Content("~/");

            if (ModelState.IsValid)
            {
                try
                {
                    // Tìm user bằng email
                    var user = await _userManager.FindByEmailAsync(model.Email);
                    if (user != null && !string.IsNullOrEmpty(user.UserName))
                    {
                        // Thử đăng nhập
                        var result = await _signInManager.PasswordSignInAsync(
                            user.UserName,
                            model.Password,
                            model.RememberMe,
                            lockoutOnFailure: false);

                        if (result.Succeeded)
                        {
                            _logger.LogInformation("User {Email} logged in.", model.Email);
                            return LocalRedirect(model.ReturnUrl);
                        }
                    }

                    ModelState.AddModelError(string.Empty, "Email hoặc mật khẩu không đúng.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during login for email {Email}", model.Email);
                    ModelState.AddModelError(string.Empty, "Có lỗi xảy ra khi đăng nhập.");
                }
            }

            return View(model);
        }

        [HttpGet]
        public IActionResult Register(string? returnUrl = null)
        {
            var model = new RegisterViewModel
            {
                ReturnUrl = returnUrl ?? Url.Content("~/")
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            model.ReturnUrl ??= Url.Content("~/");

            if (ModelState.IsValid)
            {
                try
                {
                    // Kiểm tra email đã tồn tại chưa
                    var existingUser = await _userManager.FindByEmailAsync(model.Email);
                    if (existingUser != null)
                    {
                        ModelState.AddModelError("Email", "Email này đã được sử dụng.");
                        return View(model);
                    }

                    var user = new ApplicationUser
                    {
                        UserName = model.Email,
                        Email = model.Email,
                        FullName = $"{model.FirstName} {model.LastName}",
                        PhoneNumber = model.PhoneNumber
                    };

                    var result = await _userManager.CreateAsync(user, model.Password);

                    if (result.Succeeded)
                    {
                        _logger.LogInformation("User created a new account with email {Email}.", model.Email);

                        // Đăng nhập ngay sau khi đăng ký
                        await _signInManager.SignInAsync(user, isPersistent: false);
                        return LocalRedirect(model.ReturnUrl);
                    }

                    // Xử lý lỗi từ Identity
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during registration for email {Email}", model.Email);
                    ModelState.AddModelError(string.Empty, "Có lỗi xảy ra khi đăng ký.");
                }
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            _logger.LogInformation("User logged out.");
            return RedirectToAction("Index", "Home");
        }

        public IActionResult AccessDenied()
        {
            return View();
        }
        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login");
            }

            var userBookings = await _context.Bookings
                .Include(b => b.Room)
                .Include(b => b.BookingStatus)
                .Include(b => b.Review)
                .Where(b => b.UserId == user.Id)
                .OrderByDescending(b => b.CreatedDate)
                .ToListAsync();

            var userReviews = await _context.Reviews
                .Include(r => r.Room)
                .Where(r => r.UserId == user.Id)
                .OrderByDescending(r => r.CreatedDate)
                .ToListAsync();

            // Tách tên từ FullName
            var nameParts = user.FullName?.Split(' ') ?? new[] { "", "" };
            var firstName = nameParts.Length > 0 ? nameParts[0] : "";
            var lastName = nameParts.Length > 1 ? string.Join(" ", nameParts.Skip(1)) : "";

            var model = new ProfileViewModel
            {
                FirstName = firstName,
                LastName = lastName,
                Email = user.Email ?? "",
                PhoneNumber = user.PhoneNumber ?? "",
                Birthdate = null,
                Gender = "male",
                Address = "",
                City = "",
                State = "",
                ZipCode = "",
                AvatarUrl = "/images/avatars/default.jpg",
                CreatedAt = userBookings.Any() ?
                    userBookings.Min(b => b.CreatedDate) : DateTime.Now,
                BookingsCount = userBookings.Count,
                ReviewsCount = userReviews.Count,
                LoyaltyPoints = userBookings.Sum(b => (int)(b.TotalPrice / 100000)),
                LoyaltyTier = CalculateLoyaltyTier(userBookings.Sum(b => (int)(b.TotalPrice / 100000))),
                NextTier = GetNextTier(CalculateLoyaltyTier(userBookings.Sum(b => (int)(b.TotalPrice / 100000)))),
                PointsToNextTier = CalculatePointsToNextTier(userBookings.Sum(b => (int)(b.TotalPrice / 100000))),
                NextTierProgress = CalculateTierProgress(userBookings.Sum(b => (int)(b.TotalPrice / 100000))),
                TwoFactorEnabled = await _userManager.GetTwoFactorEnabledAsync(user),
                Bookings = userBookings.Select(b => new BookingItemViewModel
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
                }).ToList(),
                Reviews = userReviews.Select(r => new ReviewItemViewModel
                {
                    Id = r.Id,
                    RoomName = r.Room?.Name ?? "Unknown Room",
                    Rating = r.Rating,
                    Comment = r.Comment,
                    CreatedAt = r.CreatedDate
                }).ToList(),
                FavoriteRooms = new List<FavoriteRoomViewModel>(),
                LoyaltyPointsHistory = userBookings.Select(b => new LoyaltyPointHistoryViewModel
                {
                    Date = b.CreatedDate,
                    Description = $"Đặt phòng {b.Room?.Name ?? "Unknown"}",
                    Points = (int)(b.TotalPrice / 100000),
                    Status = "Completed"
                }).ToList(),
                LoginActivities = new List<LoginActivityViewModel>
                {
                    new LoginActivityViewModel
                    {
                        Device = GetUserDevice(Request.Headers["User-Agent"]),
                        Location = "Current Location",
                        IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown",
                        Time = DateTime.Now,
                        Status = "Current"
                    }
                }
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(ProfileViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return RedirectToAction("Login");
                }

                // Cập nhật thông tin user
                user.FullName = $"{model.FirstName} {model.LastName}";
                user.PhoneNumber = model.PhoneNumber;

                var result = await _userManager.UpdateAsync(user);
                if (result.Succeeded)
                {
                    TempData["SuccessMessage"] = "Cập nhật thông tin thành công!";
                    return RedirectToAction("Profile");
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            // Nếu có lỗi, trả về view với model hiện tại
            return View("Profile", await GetCurrentUserProfileViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return RedirectToAction("Login");
                }

                var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
                if (result.Succeeded)
                {
                    await _signInManager.RefreshSignInAsync(user);
                    _logger.LogInformation("User changed their password successfully.");
                    TempData["SuccessMessage"] = "Đổi mật khẩu thành công!";
                    return RedirectToAction("Profile");
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            return View("Profile", await GetCurrentUserProfileViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddReview(AddReviewViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return RedirectToAction("Login");
                }

                // Kiểm tra xem booking có tồn tại và thuộc về user này không
                var booking = await _context.Bookings
                    .Include(b => b.Room)
                    .FirstOrDefaultAsync(b => b.Id == model.BookingId && b.UserId == user.Id);

                if (booking == null)
                {
                    ModelState.AddModelError(string.Empty, "Đặt phòng không tồn tại.");
                    return View("Profile", await GetCurrentUserProfileViewModel());
                }

                // Kiểm tra xem đã có review cho booking này chưa
                var existingReview = await _context.Reviews
                    .FirstOrDefaultAsync(r => r.BookingId == model.BookingId);

                if (existingReview != null)
                {
                    ModelState.AddModelError(string.Empty, "Bạn đã đánh giá đặt phòng này rồi.");
                    return View("Profile", await GetCurrentUserProfileViewModel());
                }

                var review = new Review
                {
                    Rating = model.Rating,
                    Comment = model.Comment,
                    CreatedDate = DateTime.Now,
                    UserId = user.Id,
                    RoomId = booking.RoomId,
                    BookingId = booking.Id
                };

                _context.Reviews.Add(review);
                await _context.SaveChangesAsync();

                // Cập nhật average rating cho room
                await UpdateRoomAverageRating(booking.RoomId);

                TempData["SuccessMessage"] = "Đánh giá của bạn đã được gửi thành công!";
                return RedirectToAction("Profile");
            }

            return View("Profile", await GetCurrentUserProfileViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelBooking(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login");
            }

            var booking = await _context.Bookings
                .Include(b => b.BookingStatus)
                .FirstOrDefaultAsync(b => b.Id == id && b.UserId == user.Id);

            if (booking == null)
            {
                TempData["ErrorMessage"] = "Đặt phòng không tồn tại.";
                return RedirectToAction("Profile");
            }

            // Kiểm tra xem có thể hủy booking không
            if (booking.BookingStatus?.Name != "Pending" && booking.BookingStatus?.Name != "Confirmed")
            {
                TempData["ErrorMessage"] = "Không thể hủy đặt phòng này.";
                return RedirectToAction("Profile");
            }

            if (booking.CheckIn <= DateTime.Now.AddHours(24))
            {
                TempData["ErrorMessage"] = "Chỉ có thể hủy đặt phòng trước 24 giờ so với giờ nhận phòng.";
                return RedirectToAction("Profile");
            }

            // Tìm trạng thái "Cancelled"
            var cancelledStatus = await _context.BookingStatuses
                .FirstOrDefaultAsync(s => s.Name == "Cancelled");

            if (cancelledStatus == null)
            {
                TempData["ErrorMessage"] = "Lỗi hệ thống: Không tìm thấy trạng thái hủy.";
                return RedirectToAction("Profile");
            }

            booking.BookingStatusId = cancelledStatus.Id;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Đã hủy đặt phòng thành công!";
            return RedirectToAction("Profile");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteReview(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login");
            }

            var review = await _context.Reviews
                .Include(r => r.Room)
                .FirstOrDefaultAsync(r => r.Id == id && r.UserId == user.Id);

            if (review == null)
            {
                TempData["ErrorMessage"] = "Đánh giá không tồn tại.";
                return RedirectToAction("Profile");
            }

            _context.Reviews.Remove(review);
            await _context.SaveChangesAsync();

            // Cập nhật average rating cho room
            await UpdateRoomAverageRating(review.RoomId);

            TempData["SuccessMessage"] = "Đã xóa đánh giá thành công!";
            return RedirectToAction("Profile");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleTwoFactor()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login");
            }

            var isEnabled = await _userManager.GetTwoFactorEnabledAsync(user);
            var result = await _userManager.SetTwoFactorEnabledAsync(user, !isEnabled);

            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = !isEnabled ?
                    "Đã bật xác thực hai yếu tố!" : "Đã tắt xác thực hai yếu tố!";
            }
            else
            {
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi thay đổi cài đặt bảo mật.";
            }

            return RedirectToAction("Profile");
        }

        // Helper methods
        private async Task<ProfileViewModel> GetCurrentUserProfileViewModel()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return new ProfileViewModel();

            try
            {
                var userBookings = await _context.Bookings
                    .Include(b => b.Room)
                    .Include(b => b.BookingStatus)
                    .Include(b => b.Review)
                    .Where(b => b.UserId == user.Id)
                    .OrderByDescending(b => b.CreatedDate)
                    .ToListAsync();

                var userReviews = await _context.Reviews
                    .Include(r => r.Room)
                    .Where(r => r.UserId == user.Id)
                    .OrderByDescending(r => r.CreatedDate)
                    .ToListAsync();

                var nameParts = user.FullName?.Split(' ') ?? new[] { "", "" };
                var firstName = nameParts.Length > 0 ? nameParts[0] : "";
                var lastName = nameParts.Length > 1 ? string.Join(" ", nameParts.Skip(1)) : "";

                return new ProfileViewModel
                {
                    FirstName = firstName,
                    LastName = lastName,
                    Email = user.Email ?? "",
                    PhoneNumber = user.PhoneNumber ?? "",
                    Birthdate = null,
                    Gender = "male",
                    Address = "",
                    City = "",
                    State = "",
                    ZipCode = "",
                    AvatarUrl = "/images/avatars/default.jpg",
                    CreatedAt = userBookings.Any() ? userBookings.Min(b => b.CreatedDate) : DateTime.Now,
                    BookingsCount = userBookings.Count,
                    ReviewsCount = userReviews.Count,
                    LoyaltyPoints = userBookings.Sum(b => (int)(b.TotalPrice / 100000)),
                    LoyaltyTier = CalculateLoyaltyTier(userBookings.Sum(b => (int)(b.TotalPrice / 100000))),
                    NextTier = GetNextTier(CalculateLoyaltyTier(userBookings.Sum(b => (int)(b.TotalPrice / 100000)))),
                    PointsToNextTier = CalculatePointsToNextTier(userBookings.Sum(b => (int)(b.TotalPrice / 100000))),
                    NextTierProgress = CalculateTierProgress(userBookings.Sum(b => (int)(b.TotalPrice / 100000))),
                    TwoFactorEnabled = await _userManager.GetTwoFactorEnabledAsync(user),
                    Bookings = userBookings.Select(b => new BookingItemViewModel
                    {
                        Id = b.Id,
                        BookingNumber = $"BK{b.Id:D6}",
                        RoomName = b.Room?.Name ?? "Phòng không xác định",
                        RoomImageUrl = b.Room?.ImageUrl ?? "/images/rooms/default.jpg",
                        CheckInDate = b.CheckIn,
                        CheckOutDate = b.CheckOut,
                        GuestsCount = b.Guests,
                        TotalPrice = b.TotalPrice,
                        Status = GetBookingStatusDisplay(b.BookingStatus?.Name),
                        BookingDate = b.CreatedDate,
                        HasReview = b.Review != null
                    }).ToList(),
                    Reviews = userReviews.Select(r => new ReviewItemViewModel
                    {
                        Id = r.Id,
                        RoomName = r.Room?.Name ?? "Phòng không xác định",
                        Rating = r.Rating,
                        Comment = r.Comment ?? "", // Thêm null check
                        CreatedAt = r.CreatedDate
                    }).ToList(),
                    FavoriteRooms = new List<FavoriteRoomViewModel>(),
                    LoyaltyPointsHistory = userBookings.Select(b => new LoyaltyPointHistoryViewModel
                    {
                        Date = b.CreatedDate,
                        Description = $"Đặt phòng {b.Room?.Name ?? "Không xác định"}",
                        Points = (int)(b.TotalPrice / 100000),
                        Status = "Hoàn thành"
                    }).ToList(),
                    LoginActivities = new List<LoginActivityViewModel>
            {
                new LoginActivityViewModel
                {
                    Device = GetUserDevice(Request.Headers["User-Agent"].ToString()),
                    Location = "Vị trí hiện tại",
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Không xác định",
                    Time = DateTime.Now,
                    Status = "Hiện tại"
                }
            }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy profile view model cho user {UserId}", user?.Id);
                return new ProfileViewModel();
            }
        }
        //yêu thích
        [HttpGet]
        public async Task<IActionResult> Favorites()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login");
            }
            var model = new ProfileViewModel
            {
                FavoriteRooms = new List<FavoriteRoomViewModel>() 
            };

            return View(model);
        }
        private async Task UpdateRoomAverageRating(int roomId)
        {
            var room = await _context.Rooms
                .Include(r => r.Reviews)
                .FirstOrDefaultAsync(r => r.Id == roomId);

            if (room != null && room.Reviews.Any())
            {
                room.AverageRating = room.Reviews.Average(r => r.Rating);
                await _context.SaveChangesAsync();
            }
        }

        private string CalculateLoyaltyTier(int points)
        {
            return points switch
            {
                >= 1000 => "VIP",
                >= 500 => "Gold",
                >= 100 => "Silver",
                _ => "Standard"
            };
        }

        private string GetNextTier(string currentTier)
        {
            return currentTier switch
            {
                "Standard" => "Silver",
                "Silver" => "Gold",
                "Gold" => "VIP",
                "VIP" => "VIP",
                _ => "Silver"
            };
        }

        private int CalculatePointsToNextTier(int currentPoints)
        {
            return currentPoints switch
            {
                < 100 => 100 - currentPoints,
                < 500 => 500 - currentPoints,
                < 1000 => 1000 - currentPoints,
                _ => 0
            };
        }

        private int CalculateTierProgress(int currentPoints)
        {
            return currentPoints switch
            {
                < 100 => (currentPoints * 100) / 100,
                < 500 => ((currentPoints - 100) * 100) / 400,
                < 1000 => ((currentPoints - 500) * 100) / 500,
                _ => 100
            };
        }

        private string GetBookingStatusDisplay(string? status)
        {
            return status?.ToLower() switch
            {
                "pending" => "Chờ xác nhận",
                "confirmed" => "Đã xác nhận",
                "completed" => "Đã hoàn thành",
                "cancelled" => "Đã hủy",
                _ => status ?? "Unknown"
            };
        }

        private string GetUserDevice(string userAgent)
        {
            if (userAgent.Contains("Windows"))
                return "Windows";
            else if (userAgent.Contains("Mac"))
                return "Mac";
            else if (userAgent.Contains("Android"))
                return "Android";
            else if (userAgent.Contains("iPhone") || userAgent.Contains("iPad"))
                return "iOS";
            else
                return "Unknown Device";
        }
    }

    // Additional ViewModels for form submissions
    public class ChangePasswordViewModel
    {
        public string CurrentPassword { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public class AddReviewViewModel
    {
        public int BookingId { get; set; }
        public int Rating { get; set; }
        public string Comment { get; set; } = string.Empty;
    }

}
