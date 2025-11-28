namespace QuanLiKhachSan.Models
{
    public class BookingStatus
    {
        public int Id { get; set; }
        public string Name { get; set; } // Pending, Confirmed, Completed, Cancelled
        public string Description { get; set; }
        public bool IsActive { get; set; } = true;

        public virtual ICollection<Booking> Bookings { get; set; }
    }
}
