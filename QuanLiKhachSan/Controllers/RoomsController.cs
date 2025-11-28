using Microsoft.AspNetCore.Mvc;
using QuanLiKhachSan.Services.Interfaces;
using QuanLiKhachSan.ViewModels.Room;

namespace QuanLiKhachSan.Controllers
{
    public class RoomDetailsController : Controller
    {
        private readonly IRoomService _roomService;

        public RoomDetailsController(IRoomService roomService)
        {
            _roomService = roomService;
        }

        [HttpGet]
        [Route("RoomDetails/{id}")]
        public async Task<IActionResult> Index(int id)
        {
            try
            {
                var roomDetails = await _roomService.GetRoomDetailsAsync(id);
                return View("~/Views/Rooms/Details.cshtml", roomDetails);
            }
            catch (InvalidOperationException)
            {
                return NotFound();
            }
        }
    }
}