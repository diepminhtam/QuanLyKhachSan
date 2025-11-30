using System;

namespace QuanLiKhachSan.Models
{
    public class BookingStatusHistory
    {
        public int Id { get; set; }

        // The booking this history item belongs to
        public int BookingId { get; set; }
        public virtual Booking Booking { get; set; } = null!;

        // From / To status (nullable from)
        public int? FromBookingStatusId { get; set; }
        public virtual BookingStatus? FromBookingStatus { get; set; }

        public int ToBookingStatusId { get; set; }
        public virtual BookingStatus ToBookingStatus { get; set; } = null!;

        // Who changed (user id) and optional note
        public string? ChangedByUserId { get; set; }
        public string? Note { get; set; }

        public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
    }
}
