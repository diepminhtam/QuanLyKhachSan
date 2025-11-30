using Microsoft.AspNetCore.Identity;

// Đổi tên namespace
namespace QuanLiKhachSan.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string FullName { get; set; } = string.Empty;

       public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();

        public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();
    }
}