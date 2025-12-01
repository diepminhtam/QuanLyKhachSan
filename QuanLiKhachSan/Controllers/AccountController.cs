using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
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
        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var model = await BuildProfileViewModel(user.Id);
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Favorites()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var model = new ProfileViewModel { FavoriteRooms = new List<FavoriteRoomViewModel>() };

            try
            {
                var favCookie = Request.Cookies["favorites"];
                if (!string.IsNullOrEmpty(favCookie))
                {
                    var ids = favCookie.Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(s => { int.TryParse(s, out var i); return i; })
                        .Where(i => i > 0).ToList();

                    if (ids.Any())
                    {
                        var rooms = await _context.Rooms.Where(r => ids.Contains(r.Id)).ToListAsync();
                        model.FavoriteRooms = rooms.Select(r => new FavoriteRoomViewModel
                        {
                            Id = r.Id,
                            Name = r.Name,
                            ImageUrl = r.ImageUrl ?? "/images/rooms/room-placeholder.svg",
                            Price = r.PricePerNight,
                            Rating = r.AverageRating,
                            Capacity = r.Capacity,
                            RoomType = r.RoomType
                        }).ToList();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not populate favorites for Favorites page.");
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(ProfileViewModel model)
        {
            if (!ModelState.IsValid) return View("Profile", await BuildProfileViewModel((await _userManager.GetUserAsync(User))?.Id));

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            user.FullName = $"{model.FirstName} {model.LastName}".Trim();
            user.PhoneNumber = model.PhoneNumber;

            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = "Cập nhật thông tin thành công!";
                return RedirectToAction("Profile");
            }

            foreach (var error in result.Errors) ModelState.AddModelError(string.Empty, error.Description);
            return View("Profile", await BuildProfileViewModel(user.Id));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelBooking(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var booking = await _context.Bookings
                .Include(b => b.BookingStatus)
                .FirstOrDefaultAsync(b => b.Id == id && b.UserId == user.Id);

            if (booking == null)
            {
                TempData["ErrorMessage"] = "Đặt phòng không tồn tại.";
                return RedirectToAction("Profile");
            }

            var statusName = (booking.BookingStatus?.Name ?? string.Empty).ToLower();
            var isCancelled = statusName.Contains("hủy") || statusName.Contains("cancel");
            var isConfirmed = statusName.Contains("xác nhận") || statusName.Contains("confirmed");

            if (isCancelled)
            {
                TempData["ErrorMessage"] = "Đặt phòng đã được hủy trước đó.";
                return RedirectToAction("Profile");
            }

            if (isConfirmed)
            {
                TempData["ErrorMessage"] = "Không thể hủy đơn đã được xác nhận. Vui lòng liên hệ quản trị để được hỗ trợ.";
                return RedirectToAction("Profile");
            }

            if (booking.CheckIn <= DateTime.Now.AddHours(24))
            {
                TempData["ErrorMessage"] = "Chỉ có thể hủy đặt phòng trước 24 giờ so với giờ nhận phòng.";
                return RedirectToAction("Profile");
            }

            var cancelledStatus = await _context.BookingStatuses.FirstOrDefaultAsync(s => s.Name == "Cancelled")
                                  ?? await _context.BookingStatuses.FirstOrDefaultAsync();
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

        private async Task<ProfileViewModel> BuildProfileViewModel(string userId)
        {
            if (string.IsNullOrEmpty(userId)) return new ProfileViewModel();

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null) return new ProfileViewModel();

            var bookings = await _context.Bookings
                .Include(b => b.Room)
                .Include(b => b.BookingStatus)
                .Where(b => b.UserId == userId)
                .OrderByDescending(b => b.CreatedDate)
                .ToListAsync();

            var model = new ProfileViewModel
            {
                FirstName = (user.FullName ?? string.Empty).Split(' ').FirstOrDefault() ?? string.Empty,
                LastName = string.Join(' ', (user.FullName ?? string.Empty).Split(' ').Skip(1)),
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                BookingsCount = bookings.Count,
                Bookings = bookings.Select(b => new BookingItemViewModel
                {
                    Id = b.Id,
                    BookingNumber = $"BK{b.Id:D6}",
                    RoomName = b.Room?.Name ?? string.Empty,
                    CheckInDate = b.CheckIn,
                    CheckOutDate = b.CheckOut,
                    TotalPrice = b.TotalPrice,
                    Status = b.BookingStatus?.Name ?? string.Empty
                }).ToList(),
                FavoriteRooms = new List<FavoriteRoomViewModel>()
            };

            return model;
        }
    }
}
