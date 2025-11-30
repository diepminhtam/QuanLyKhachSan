using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using QuanLiKhachSan.Data;
using QuanLiKhachSan.Models;
using QuanLiKhachSan.ViewModels.Account;

namespace QuanLiKhachSan.Controllers
{
    // Clean replacement controller that explicitly routes to /Account/*
    [Route("Account")]
    public class AccountControllerClean : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ILogger<AccountControllerClean> _logger;
        private readonly ApplicationDbContext _context;

        public AccountControllerClean(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ILogger<AccountControllerClean> logger,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
            _context = context;
        }

        [HttpGet]
        public IActionResult Index() => Content("Account controller (clean) is present. Use /Account/Profile for profile.");

        [HttpGet("Profile")]
        public async Task<IActionResult> Profile()
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null) return RedirectToAction("Login", "Account");

                // Load bookings and reviews
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

            var bookingsVm = userBookings.Select(b => new BookingItemViewModel
            {
                Id = b.Id,
                BookingNumber = $"BK{b.Id:D6}",
                RoomName = b.Room?.Name ?? "Phòng không xác định",
                RoomImageUrl = b.Room?.ImageUrl ?? "/images/rooms/default.jpg",
                CheckInDate = b.CheckIn,
                CheckOutDate = b.CheckOut,
                GuestsCount = b.Guests,
                TotalPrice = b.TotalPrice,
                Status = b.BookingStatus?.Name ?? "",
                BookingDate = b.CreatedDate,
                HasReview = b.Review != null
            }).ToList();

            var reviewsVm = userReviews.Select(r => new ReviewItemViewModel
            {
                Id = r.Id,
                RoomName = r.Room?.Name ?? "Phòng không xác định",
                Rating = r.Rating,
                Comment = r.Comment ?? string.Empty,
                CreatedAt = r.CreatedDate
            }).ToList();

            // Favorites from cookie
            var favRooms = new List<FavoriteRoomViewModel>();
            try
            {
                var fav = Request.Cookies["favorites"];
                if (!string.IsNullOrEmpty(fav))
                {
                    var ids = fav.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(s => { int.TryParse(s, out var i); return i; }).Where(i => i > 0).ToList();
                    if (ids.Any())
                    {
                        var rooms = await _context.Rooms.Where(r => ids.Contains(r.Id)).ToListAsync();
                        favRooms = rooms.Select(r => new FavoriteRoomViewModel
                        {
                            Id = r.Id,
                            Name = r.Name,
                            ImageUrl = r.ImageUrl ?? "/images/rooms/default.jpg",
                            Price = r.PricePerNight,
                            Rating = r.AverageRating,
                            Capacity = r.Capacity,
                            RoomType = r.RoomType
                        }).ToList();
                    }
                }
            }
            catch { }

            // Loyalty calculations
            var loyaltyPoints = userBookings.Sum(b => (int)(b.TotalPrice / 100000));
            var loyaltyTier = CalculateLoyaltyTier(loyaltyPoints);
            var nextTier = GetNextTier(loyaltyTier);
            var pointsToNext = CalculatePointsToNextTier(loyaltyPoints);
            var nextTierProgress = CalculateTierProgress(loyaltyPoints);

            var model = new ProfileViewModel
            {
                FirstName = (user.FullName ?? string.Empty).Split(' ').FirstOrDefault() ?? string.Empty,
                LastName = string.Join(' ', (user.FullName ?? string.Empty).Split(' ').Skip(1)),
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                AvatarUrl = "/images/avatars/default.jpg",
                CreatedAt = userBookings.Any() ? userBookings.Min(b => b.CreatedDate) : DateTime.Now,
                BookingsCount = userBookings.Count,
                ReviewsCount = userReviews.Count,
                LoyaltyPoints = loyaltyPoints,
                Bookings = bookingsVm,
                Reviews = reviewsVm,
                FavoriteRooms = favRooms,
                LoyaltyTier = loyaltyTier,
                NextTier = nextTier,
                PointsToNextTier = pointsToNext,
                NextTierProgress = nextTierProgress,
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
                        Status = "Current"
                    }
                },
                TwoFactorEnabled = await _userManager.GetTwoFactorEnabledAsync(user)
            };

            // Render the existing view located at Views/Account/Profile.cshtml
                return View("~/Views/Account/Profile.cshtml", model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error building profile view for user");
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        // Helpers for loyalty and device detection
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

        private int CalculatePointsToNextTier(int points)
        {
            return points switch
            {
                < 100 => 100 - points,
                < 500 => 500 - points,
                < 1000 => 1000 - points,
                _ => 0
            };
        }

        private int CalculateTierProgress(int points)
        {
            return points switch
            {
                < 100 => (points * 100) / 100,
                < 500 => ((points - 100) * 100) / 400,
                < 1000 => ((points - 500) * 100) / 500,
                _ => 100
            };
        }

        private string GetUserDevice(string userAgent)
        {
            if (string.IsNullOrEmpty(userAgent)) return "Unknown";
            if (userAgent.Contains("Windows")) return "Windows";
            if (userAgent.Contains("Mac")) return "Mac";
            if (userAgent.Contains("Android")) return "Android";
            if (userAgent.Contains("iPhone") || userAgent.Contains("iPad")) return "iOS";
            return "Unknown Device";
        }
    }
}
