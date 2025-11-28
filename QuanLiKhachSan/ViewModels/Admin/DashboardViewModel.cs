namespace QuanLiKhachSan.ViewModels.Admin
{
    public class DashboardViewModel
    {
        public decimal TotalRevenue { get; set; }
        public int TotalBookings { get; set; }
        public double RoomOccupancyRate { get; set; }
        public int TotalCustomers { get; set; }
        // Optionally, add more metrics as needed
    }
}