
namespace QuanLiKhachSan.Models
{
    public class Booking
    {
        public int Id { get; set; }
        public DateTime CheckIn { get; set; }
        public DateTime CheckOut { get; set; }
        public int Guests { get; set; }
        public decimal TotalPrice { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime? CompletedDate { get; set; }

        // BookingStatus
        public int BookingStatusId { get; set; }
        public virtual BookingStatus BookingStatus { get; set; } = null!;

        // User
        public string UserId { get; set; } = string.Empty;
        public virtual ApplicationUser User { get; set; } = null!;

        // Room
        public int RoomId { get; set; }
        public virtual Room Room { get; set; } = null!;

        // Payments (1 Booking có nhiều Payment)
        public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

        // Review (1 Booking có 1 Review)
        public virtual Review? Review { get; set; }
    }
}