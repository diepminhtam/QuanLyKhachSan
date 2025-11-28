namespace QuanLiKhachSan.Models
{
    public class Service
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        public string ImageUrl { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;

        public decimal Price { get; set; }
        public string Duration { get; set; } = string.Empty;

        // Lưu danh sách features dạng string: "Wifi, Hồ bơi, Spa"
        public string Features { get; set; } = string.Empty;
    }
}
