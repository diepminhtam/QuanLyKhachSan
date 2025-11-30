namespace QuanLiKhachSan.ViewModels.Booking
{
    public class BookingListViewModel
    {
        public string Status { get; set; } = string.Empty; // Để lọc: Tất cả, Chờ xác nhận, Đã xác nhận, Hoàn thành, Đã hủy
        public List<BookingItemViewModel> Bookings { get; set; } = new List<BookingItemViewModel>();

        // Chỉ hiển thị các đặt phòng phù hợp với bộ lọc trạng thái
        public List<BookingItemViewModel> FilteredBookings
        {
            get
            {
                if (string.IsNullOrEmpty(Status))
                    return Bookings;

                return Bookings.Where(b => b.Status.Equals(Status, StringComparison.OrdinalIgnoreCase)).ToList();
            }
        }

        // Tính toán số lượng đặt phòng theo từng trạng thái
        public int GetBookingCountByStatus(string status)
        {
            if (string.IsNullOrEmpty(status))
                return Bookings.Count;

            return Bookings.Count(b => b.Status.Equals(status, StringComparison.OrdinalIgnoreCase));
        }
    }

    // Sử dụng lại BookingItemViewModel đã có trong ProfileViewModel, hoặc định nghĩa lại ở đây
    public class BookingItemViewModel
    {
        public int Id { get; set; }
        public DateTime CheckInDate { get; set; }
        public DateTime CheckOutDate { get; set; }
        public int GuestsCount { get; set; }
        public decimal TotalPrice { get; set; }
        public DateTime BookingDate { get; set; }
        public bool HasReview { get; set; }
        public string BookingNumber { get; set; } = string.Empty;
        public string RoomName { get; set; } = string.Empty;
        public string RoomImageUrl { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;

        // Helper properties
        public int NightCount => (CheckOutDate - CheckInDate).Days;
        public bool CanCancel => Status == "Chờ xác nhận" || Status == "Đã xác nhận" && CheckInDate > DateTime.Now;
        public bool CanReview => Status == "Hoàn thành" && !HasReview;
        public bool IsUpcoming => CheckInDate > DateTime.Now && Status != "Đã hủy";
        public bool IsActive => CheckInDate <= DateTime.Now && CheckOutDate >= DateTime.Now && Status != "Đã hủy";
        public bool IsPast => CheckOutDate < DateTime.Now && Status != "Đã hủy";
    }
}
