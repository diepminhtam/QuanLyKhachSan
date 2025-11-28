namespace QuanLiKhachSan.ViewModels.Admin
{
    public class BookingsViewModel
    {
        public List<BookingItemViewModel> Bookings { get; set; } = new List<BookingItemViewModel>();
        public string SearchTerm { get; set; }
        public string Status { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int TotalBookings { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalPages => (int)Math.Ceiling(TotalBookings / (double)PageSize);

        // Booking counts by status
        public int PendingCount { get; set; }
        public int ConfirmedCount { get; set; }
        public int CompletedCount { get; set; }
        public int CancelledCount { get; set; }

    }

    public class BookingItemViewModel
    {
        public int Id { get; set; }
        public string BookingNumber { get; set; }
        public string CustomerName { get; set; }
        public string CustomerEmail { get; set; }
        public string RoomName { get; set; }
        public DateTime CheckInDate { get; set; }
        public DateTime CheckOutDate { get; set; }
        public int GuestCount { get; set; }
        public decimal TotalPrice { get; set; }
        public string Status { get; set; }
        public string PaymentStatus { get; set; }
        public DateTime BookingDate { get; set; }
        
    }
}

