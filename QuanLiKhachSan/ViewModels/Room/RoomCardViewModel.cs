namespace QuanLiKhachSan.ViewModels.Room
{
    public class RoomCardViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string? Description { get; set; }
        public string ImageUrl { get; set; } = "";
        public string RoomType { get; set; } = "";
        public int Capacity { get; set; }
        public decimal PricePerNight { get; set; }
        public double AverageRating { get; set; }
        public int ReviewsCount { get; set; }
        public List<string> Amenities { get; set; } = new();

        // Trạng thái còn phòng hay không
        public bool IsAvailable { get; set; }
    }
}
