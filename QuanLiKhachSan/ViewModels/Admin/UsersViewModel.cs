namespace QuanLiKhachSan.ViewModels.Admin
{
    public class UsersViewModel
    {
        public List<UserItemViewModel> Users { get; set; } = new List<UserItemViewModel>();
        public string SearchTerm { get; set; }
        public string Status { get; set; }
        public string UserType { get; set; }
        public int TotalUsers { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalPages => (int)Math.Ceiling(TotalUsers / (double)PageSize);

        // User counts by status
        public int ActiveCount { get; set; }
        public int NewCount { get; set; } // Trong 30 ngày
        public int VipCount { get; set; }
        public int InactiveCount { get; set; }
    }

    public class UserItemViewModel
    {
        public string Id { get; set; }
        public string UserId { get; set; } // ID hiển thị: #UID001
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string FullName => $"{FirstName} {LastName}";
        public string Initials => string.IsNullOrEmpty(FirstName) || string.IsNullOrEmpty(LastName)
            ? "??"
            : $"{FirstName[0]}{LastName[0]}";
        public string Email { get; set; }
        public string Phone { get; set; }
        public DateTime RegisterDate { get; set; }
        public int BookingsCount { get; set; }
        public decimal TotalSpending { get; set; }
        public string Status { get; set; } // "active", "inactive", "pending"
        public string UserType { get; set; } // "normal", "vip"
        public string Address { get; set; }
        public int LoyaltyPoints { get; set; }
        public DateTime? LastLoginDate { get; set; }
    }

    public class UserDetailViewModel : UserItemViewModel
    {
        public List<UserBookingViewModel> RecentBookings { get; set; } = new List<UserBookingViewModel>();
        public List<UserActivityViewModel> RecentActivities { get; set; } = new List<UserActivityViewModel>();
    }

    public class UserBookingViewModel
    {
        public string BookingNumber { get; set; }
        public string RoomName { get; set; }
        public DateTime CheckIn { get; set; }
        public DateTime CheckOut { get; set; }
        public string Status { get; set; }
        public decimal TotalPrice { get; set; }
    }

    public class UserActivityViewModel
    {
        public DateTime Date { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Type { get; set; } // "login", "create", "update", "review", etc.
    }
}

