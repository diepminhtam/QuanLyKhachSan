using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLiKhachSan.Data;

namespace QuanLiKhachSan.Controllers.Admin
{
    [Authorize(Roles = "Admin")]
    [Route("Admin/[controller]/[action]")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Admin/Admin/Dashboard
        public async Task<IActionResult> Dashboard()
        {
            var totalRooms = await _context.Rooms.CountAsync();
            var totalBookings = await _context.Bookings.CountAsync();
            var activeBookings = await _context.Bookings
                .Where(b => b.CheckIn <= DateTime.Now && b.CheckOut >= DateTime.Now)
                .CountAsync();
            var totalRevenue = await _context.Bookings.SumAsync(b => (decimal?)b.TotalPrice) ?? 0m;
            var recentBookings = await _context.Bookings
                .Include(b => b.Room)
                .Include(b => b.User)
                .OrderByDescending(b => b.CreatedDate)
                .Take(10)
                .ToListAsync();

            ViewData["TotalRooms"] = totalRooms;
            ViewData["TotalBookings"] = totalBookings;
            ViewData["ActiveBookings"] = activeBookings;
            ViewData["TotalRevenue"] = totalRevenue;

            return View(recentBookings);
        }
    }
}
