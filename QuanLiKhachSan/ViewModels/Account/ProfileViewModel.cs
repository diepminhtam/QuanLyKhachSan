namespace QuanLiKhachSan.ViewModels.Account
{
    public class ProfileViewModel
    {
        // Thông tin cá nhân
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Gender { get; set; }
        public DateTime? Birthdate { get; set; } // Thêm ngày sinh
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? ZipCode { get; set; }
        public string? AvatarUrl { get; set; }
        public DateTime CreatedAt { get; set; }

        // Thống kê
        public int BookingsCount { get; set; }
        public int ReviewsCount { get; set; }
        public int LoyaltyPoints { get; set; }

        // Danh sách đặt phòng
        public List<BookingItemViewModel> Bookings { get; set; } = new();

        // Danh sách đánh giá
        public List<ReviewItemViewModel> Reviews { get; set; } = new();

        // Danh sách phòng yêu thích
        public List<FavoriteRoomViewModel> FavoriteRooms { get; set; } = new();

        // Thông tin điểm thưởng
        public string? LoyaltyTier { get; set; }
        public string? NextTier { get; set; }
        public int PointsToNextTier { get; set; }
        public int NextTierProgress { get; set; }
        public List<LoyaltyPointHistoryViewModel> LoyaltyPointsHistory { get; set; } = new();

        // Cài đặt bảo mật
        public bool TwoFactorEnabled { get; set; }
        public List<LoginActivityViewModel> LoginActivities { get; set; } = new();
    }

    public class BookingItemViewModel
    {
        public int Id { get; set; }
        public DateTime CheckInDate { get; set; }
        public DateTime CheckOutDate { get; set; }
        public int GuestsCount { get; set; }
        public decimal TotalPrice { get; set; }
            public string BookingNumber { get; set; } = string.Empty;
            public string RoomName { get; set; } = string.Empty;
            public string RoomImageUrl { get; set; } = string.Empty;
            public string Status { get; set; } = string.Empty;
        public DateTime BookingDate { get; set; }
        public bool HasReview { get; set; }
    }

    public class ReviewItemViewModel
    {
        public int Id { get; set; }
        public string RoomName { get; set; } = string.Empty;
        public int Rating { get; set; }
        public string Comment { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class FavoriteRoomViewModel
    {
        public int Id { get; set; }
        public decimal Price { get; set; }
        public double Rating { get; set; }
        public int Capacity { get; set; }
        public string Name { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public string RoomType { get; set; } = string.Empty;
    }

    public class LoyaltyPointHistoryViewModel
    {
        public DateTime Date { get; set; }
            public string Description { get; set; } = string.Empty;
            public int Points { get; set; }
            public string Status { get; set; } = string.Empty;
    }

    public class LoginActivityViewModel
    {
        public string Device { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string IpAddress { get; set; } = string.Empty;
        public DateTime Time { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}
