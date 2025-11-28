using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLiKhachSan.Data;
using QuanLiKhachSan.Models;
using QuanLiKhachSan.Services.Interfaces;
using QuanLiKhachSan.ViewModels;
using QuanLiKhachSan.ViewModels.Room;
using System.Diagnostics;

namespace QuanLiKhachSan.Controllers
{
    public class HomeController : Controller
    {
        private readonly IRoomService _roomService;
        private readonly ApplicationDbContext _context;

        public HomeController(IRoomService roomService, ApplicationDbContext context)
        {
            _roomService = roomService;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var featuredRooms = await _roomService.GetFeaturedRoomsAsync(4);
            return View(featuredRooms);
        }

        public async Task<IActionResult> Rooms(RoomListViewModel searchModel)
        {
            // The controller just calls the service. All logic is in the service.
            var viewModel = await _roomService.GetFilteredRoomsAsync(searchModel);
            return View(viewModel);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        public IActionResult Services()
        {
            var services = _context.Services.ToList();
            var viewModels = services.Select(s => new ServiceViewModel
            {
                Id = s.Id,
                Name = s.Name,
                Description = s.Description,
                ImageUrl = s.ImageUrl,
                Price = s.Price,
                Duration = s.Duration,
                Icon = s.Icon,
                Features = string.IsNullOrEmpty(s.Features)
                    ? new List<string>()
                    : s.Features.Split(',').Select(f => f.Trim()).ToList()
            }).ToList();
            return View(viewModels);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
        public IActionResult Gallery()
        {
            return View();
        }
        public IActionResult Contact()
        {
            return View();
        }
    }
}