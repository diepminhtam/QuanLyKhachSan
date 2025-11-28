namespace QuanLiKhachSan.Models
{
    public class Review
    {
        public int Id { get; set; }
        public int Rating { get; set; } // 1-5
        public string Comment { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        // THÊM 2 PROPERTIES BỊ THIẾU
        public DateTime UpdatedDate { get; set; } = DateTime.Now;
        public string Status { get; set; } = "Active"; // Active, Inactive, Pending

        // User
        public string UserId { get; set; } = string.Empty;
        public virtual ApplicationUser User { get; set; } = null!;

        // Room
        public int RoomId { get; set; }
        public virtual Room Room { get; set; } = null!;

        // Booking
        public int BookingId { get; set; }
        public virtual Booking Booking { get; set; } = null!;
    }
}