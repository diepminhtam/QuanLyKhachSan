namespace QuanLiKhachSan.ViewModels.Admin
{
    public class ReviewViewModel
    {
        public int Id { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
        public string RoomName { get; set; } = string.Empty;
        public string RoomType { get; set; } = string.Empty;
        public int Rating { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Comment { get; set; } = string.Empty;
        public string Status { get; set; } = "Pending";
        public DateTime CreatedDate { get; set; }
    }
}