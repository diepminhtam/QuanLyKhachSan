namespace QuanLiKhachSan.ViewModels.Admin
{ 
    public class RoomsViewModel
    {
        public List<RoomItemViewModel> Rooms { get; set; } = new List<RoomItemViewModel>();
        public string SearchTerm { get; set; }
        public string RoomType { get; set; }
        public string Status { get; set; }
        public string SortBy { get; set; }
        public int TotalRooms { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalPages => (int)Math.Ceiling(TotalRooms / (double)PageSize);

        // Room counts by status
        public int AvailableCount { get; set; }
        public int OccupiedCount { get; set; }
        public int MaintenanceCount { get; set; }
        public int CleaningCount { get; set; }
    }

    public class RoomItemViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string RoomType { get; set; }
        public decimal PricePerNight { get; set; }
        public int Capacity { get; set; }
        public string Status { get; set; }
        public int Floor { get; set; }
        public string RoomNumber { get; set; }
        public double Rating { get; set; }
        public string ImageUrl { get; set; }
        public string Description { get; set; }
        public List<string> Amenities { get; set; } = new List<string>();
    }
}

