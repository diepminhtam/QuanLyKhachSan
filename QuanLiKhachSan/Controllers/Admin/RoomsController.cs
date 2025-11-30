using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLiKhachSan.Data;
using QuanLiKhachSan.Models;

namespace QuanLiKhachSan.Controllers.Admin
{
    [Authorize(Roles = "Admin")]
    [Route("Admin/[controller]/[action]")]
    public class RoomsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public RoomsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Admin/Rooms/Index
        public async Task<IActionResult> Index(int page = 1, int pageSize = 20)
        {
            var query = _context.Rooms.AsNoTracking().OrderBy(r => r.Id);
            var total = await query.CountAsync();
            var rooms = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            ViewData["Total"] = total;
            ViewData["Page"] = page;
            ViewData["PageSize"] = pageSize;

            return View(rooms);
        }

        // GET: Admin/Rooms/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var room = await _context.Rooms.Include(r => r.Reviews).FirstOrDefaultAsync(r => r.Id == id);
            if (room == null) return NotFound();
            return View(room);
        }

        // GET: Admin/Rooms/Create
        public IActionResult Create()
        {
            return View(new Room());
        }

        // POST: Admin/Rooms/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Room model)
        {
            if (!ModelState.IsValid) return View(model);

            _context.Rooms.Add(model);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Phòng đã được tạo thành công.";
            return RedirectToAction(nameof(Index));
        }

        // GET: Admin/Rooms/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var room = await _context.Rooms.FindAsync(id);
            if (room == null) return NotFound();
            return View(room);
        }

        // POST: Admin/Rooms/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Room model)
        {
            if (id != model.Id) return BadRequest();
            if (!ModelState.IsValid) return View(model);

            var room = await _context.Rooms.FindAsync(id);
            if (room == null) return NotFound();

            room.Name = model.Name;
            room.Description = model.Description;
            room.ImageUrl = model.ImageUrl;
            room.PricePerNight = model.PricePerNight;
            room.Capacity = model.Capacity;
            room.RoomType = model.RoomType;
            room.IsAvailable = model.IsAvailable;

            _context.Rooms.Update(room);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Phòng đã được cập nhật.";
            return RedirectToAction(nameof(Index));
        }

        // GET: Admin/Rooms/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            var room = await _context.Rooms.FindAsync(id);
            if (room == null) return NotFound();
            return View(room);
        }

        // POST: Admin/Rooms/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var room = await _context.Rooms.FindAsync(id);
            if (room == null) return NotFound();

            _context.Rooms.Remove(room);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Phòng đã được xoá.";
            return RedirectToAction(nameof(Index));
        }
    }
}
