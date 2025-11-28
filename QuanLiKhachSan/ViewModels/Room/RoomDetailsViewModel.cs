namespace QuanLiKhachSan.ViewModels.Room
{
    public class RoomFeatureViewModel
    {
        public string Name { get; set; } = "";
        public string Icon { get; set; } = "";
    }
    public class ReviewViewModel
    {
        public string UserName { get; set; } = "";
        public string? UserAvatarUrl { get; set; }
        public int Rating { get; set; }
        public DateTime Date { get; set; }
        public string Comment { get; set; } = "";
    }
    public class SimilarRoomViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string ImageUrl { get; set; } = "";
        public decimal Price { get; set; }
        public double Rating { get; set; }
    }
    public class RoomDetailsViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public string MainImageUrl { get; set; } = "";
        public List<string> ImageUrls { get; set; } = new();
        public string RoomType { get; set; } = "";
        public int Capacity { get; set; }
        public decimal PricePerNight { get; set; }
        public double AverageRating { get; set; }
        public int ReviewsCount { get; set; }
        public List<RoomFeatureViewModel> Features { get; set; } = new();
        public List<ReviewViewModel> Reviews { get; set; } = new();
        public List<SimilarRoomViewModel> SimilarRooms { get; set; } = new();
    }
}
