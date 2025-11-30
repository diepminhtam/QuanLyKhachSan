namespace QuanLiKhachSan.ViewModels.Admin
{
    public class DashboardViewModel
    {
        public decimal TotalRevenue { get; set; }
        public int TotalBookings { get; set; }
        public double RoomOccupancyRate { get; set; }
        public int TotalCustomers { get; set; }

        // Dữ liệu động cho dashboard
        public List<RecentBookingDto> RecentBookings { get; set; } = new();
        public List<RecentReviewDto> RecentReviews { get; set; } = new();
        public List<TopCustomerDto> TopCustomers { get; set; } = new();
        public RoomStatusCountsDto RoomStatusCounts { get; set; } = new();
        public List<NotificationDto> Notifications { get; set; } = new();

        public class RecentBookingDto
        {
            public string BookingNumber { get; set; }
            public string CustomerName { get; set; }
            public string RoomName { get; set; }
            public DateTime CheckIn { get; set; }
            public DateTime CheckOut { get; set; }
            public string Status { get; set; }
            public decimal TotalPrice { get; set; }
        }

        public class RecentReviewDto
        {
            public string CustomerName { get; set; }
            public string Initials { get; set; }
            public DateTime CreatedDate { get; set; }
            public int Rating { get; set; }
            public string RoomName { get; set; }
            public string Comment { get; set; }
        }

        public class TopCustomerDto
        {
            public string CustomerName { get; set; }
            public string Initials { get; set; }
            public string Email { get; set; }
            public int BookingCount { get; set; }
            public decimal TotalRevenue { get; set; }
        }

        public class RoomStatusCountsDto
        {
            public int Available { get; set; }
            public int Occupied { get; set; }
            public int Cleaning { get; set; }
            public int Maintenance { get; set; }
        }

        public class NotificationDto
        {
            public string IconClass { get; set; }
            public string BgClass { get; set; }
            public string Title { get; set; }
            public string Text { get; set; }
            public string TimeAgo { get; set; }
            public bool Unread { get; set; }
        }
    }
}