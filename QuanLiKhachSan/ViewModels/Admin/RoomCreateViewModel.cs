
using System.ComponentModel.DataAnnotations;

namespace QuanLiKhachSan.ViewModels.Admin
{
    public class RoomCreateViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập tên phòng")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn loại phòng")]
        public string RoomType { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập giá phòng")]
        [Range(0, double.MaxValue, ErrorMessage = "Giá phòng phải lớn hơn 0")]
        public decimal PricePerNight { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập sức chứa")]
        [Range(1, 10, ErrorMessage = "Sức chứa phải từ 1-10 người")]
        public int Capacity { get; set; }

        public string BedType { get; set; }

        public int Size { get; set; }

        public string Floor { get; set; }

        public string RoomNumber { get; set; }

        public bool IsAvailable { get; set; } = true;

        public string View { get; set; }

        public bool IncludeBreakfast { get; set; }

        public string Description { get; set; }

        public List<string> Amenities { get; set; } = new List<string>();

        [Required(ErrorMessage = "Vui lòng tải lên ảnh chính cho phòng")]
        public IFormFile MainImage { get; set; }

        public IFormFileCollection AdditionalImages { get; set; }
    }
}
