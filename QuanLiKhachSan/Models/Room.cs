
namespace QuanLiKhachSan.Models
{
    public class Room
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal PricePerNight { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
        public int Capacity { get; set; }
        public string RoomType { get; set; } = string.Empty;
        public bool IsAvailable { get; set; } = true;
        public double AverageRating { get; set; } = 0;

        public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();
        public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();
    }
}