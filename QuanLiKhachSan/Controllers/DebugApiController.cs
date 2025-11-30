using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLiKhachSan.Data;

namespace QuanLiKhachSan.Controllers
{
    [ApiController]
    [Route("api/debug")]
    public class DebugApiController : ControllerBase
    {
        private readonly ApplicationDbContext _db;

        public DebugApiController(ApplicationDbContext db)
        {
            _db = db;
        }

        // GET: /api/debug/latest-booking
        [HttpGet("latest-booking")]
        public async Task<IActionResult> GetLatestBooking()
        {
            var booking = await _db.Bookings
                .Include(b => b.Room)
                .Include(b => b.Payments)
                .Include(b => b.BookingStatus)
                .OrderByDescending(b => b.Id)
                .FirstOrDefaultAsync();

            if (booking == null) return NotFound(new { message = "No bookings found" });

            var histories = await _db.BookingStatusHistories
                .Where(h => h.BookingId == booking.Id)
                .Include(h => h.ToBookingStatus)
                .Include(h => h.FromBookingStatus)
                .OrderBy(h => h.ChangedAt)
                .ToListAsync();

            return Ok(new {
                booking = new {
                    booking.Id,
                    booking.BookingStatusId,
                    Status = booking.BookingStatus?.Name,
                    booking.RoomId,
                    RoomName = booking.Room?.Name,
                    booking.CheckIn,
                    booking.CheckOut,
                    booking.Guests,
                    booking.TotalPrice,
                    booking.CreatedDate,
                    booking.UserId
                },
                payments = booking.Payments.Select(p => new { p.Id, p.Amount, p.PaymentDate, p.PaymentStatusId }),
                histories = histories.Select(h => new {
                    h.Id,
                    h.FromBookingStatusId,
                    From = h.FromBookingStatus?.Name,
                    h.ToBookingStatusId,
                    To = h.ToBookingStatus?.Name,
                    h.ChangedByUserId,
                    h.Note,
                    h.ChangedAt
                })
            });
        }
    }
}
