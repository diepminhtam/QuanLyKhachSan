using Microsoft.AspNetCore.Http.Features;

namespace QuanLiKhachSan.ViewModels.RoomsViewModel
{
    public class RoomDetailsViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string MainImageUrl { get; set; }
        public List<string> ImageUrls { get; set; } = new List<string>();
        public string RoomType { get; set; }
        public string BedType { get; set; }
        public int Size { get; set; }
        public int Capacity { get; set; }
        public decimal PricePerNight { get; set; }
        public decimal OriginalPrice { get; set; }
        public double AverageRating { get; set; }
        public int ReviewsCount { get; set; }
        public bool IsFavorited { get; set; }
        public bool IncludeBreakfast { get; set; }
        public string Floor { get; set; }
        public string Building { get; set; }
        public string View { get; set; }
        public Dictionary<int, int> RatingDistribution { get; set; } = new Dictionary<int, int>();
        public List<RoomFeature> Features { get; set; } = new List<RoomFeature>();
        public List<ReviewViewModel> Reviews { get; set; } = new List<ReviewViewModel>();
        public List<SimilarRoomViewModel> SimilarRooms { get; set; } = new List<SimilarRoomViewModel>();
    }

    public class ReviewViewModel
    {
        public string UserName { get; set; }
        public string UserAvatarUrl { get; set; }
        public int Rating { get; set; }
        public DateTime Date { get; set; }
        public string Comment { get; set; }
    }

    public class SimilarRoomViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string ImageUrl { get; set; }
        public decimal Price { get; set; }
        public double Rating { get; set; }
        public int Size { get; set; }
        public int Capacity { get; set; }
    }
}
