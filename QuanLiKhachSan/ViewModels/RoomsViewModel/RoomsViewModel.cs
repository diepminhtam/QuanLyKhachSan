namespace QuanLiKhachSan.ViewModels.RoomsViewModel
{
    public class RoomsViewModel
    {
        public List<RoomDisplayViewModel> Rooms { get; set; } = new List<RoomDisplayViewModel>();
        public int TotalRooms { get; set; }
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; } = 1;
        public DateTime CheckIn { get; set; } = DateTime.Today;
        public DateTime CheckOut { get; set; } = DateTime.Today.AddDays(1);
        public int Adults { get; set; } = 2;
        public int Children { get; set; } = 0;
    }

    public class RoomDisplayViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string ImageUrl { get; set; }
        public string RoomType { get; set; }
        public string BedType { get; set; }
        public int Size { get; set; }
        public int Capacity { get; set; }
        public decimal PricePerNight { get; set; }
        public decimal OriginalPrice { get; set; }
        public bool IsPromotion { get; set; }
        public double AverageRating { get; set; }
        public int ReviewsCount { get; set; }
        public bool IsFavorited { get; set; }
        public bool IncludeBreakfast { get; set; }
        public List<RoomFeature> Features { get; set; } = new List<RoomFeature>();
    }

    public class RoomFeature
    {
        public string Name { get; set; }
        public string Icon { get; set; }
    }
}

