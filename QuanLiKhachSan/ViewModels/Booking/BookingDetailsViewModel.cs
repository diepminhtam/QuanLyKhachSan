namespace QuanLiKhachSan.ViewModels.Booking
{
    public class BookingDetailsViewModel
    {
        public int Id { get; set; }
        public string BookingNumber { get; set; }
        public string Status { get; set; }
        public DateTime BookingDate { get; set; }

        // Thông tin phòng
        public int RoomId { get; set; }
        public string RoomName { get; set; }
        public string RoomType { get; set; }
        public string RoomDescription { get; set; }
        public string RoomImageUrl { get; set; }
        public double RoomRating { get; set; }
        public int RoomCapacity { get; set; }
        public int RoomSize { get; set; }
        public string BedType { get; set; }
        public string Floor { get; set; }
        public string Building { get; set; }

        // Thông tin lưu trú
        public DateTime CheckInDate { get; set; }
        public DateTime CheckOutDate { get; set; }
        public int GuestsCount { get; set; }
        public string SpecialRequests { get; set; }
        public int NightCount => (CheckOutDate - CheckInDate).Days;

        // Thông tin khách hàng
        public string GuestName { get; set; }
        public string GuestEmail { get; set; }
        public string GuestPhone { get; set; }

        // Thông tin thanh toán
        public decimal RoomPrice { get; set; } // Tổng giá phòng cho tất cả đêm
        public decimal ServiceFee { get; set; }
        public decimal TaxFee { get; set; }
        public decimal Discount { get; set; }
        public decimal TotalPrice { get; set; }
        public string PaymentMethod { get; set; }
        public string PaymentStatus { get; set; }
        public string PaymentDetails { get; set; } // Thông tin chi tiết về thanh toán nếu có

        // Chính sách hủy phòng
        public bool IsCancellable { get; set; }
        public DateTime FreeCancellationDeadline { get; set; }

        // Đánh giá
        public bool CanReview { get; set; }
        public bool HasReview { get; set; }

        // Lịch sử hoạt động đặt phòng
        public List<BookingActivityViewModel> BookingActivities { get; set; } = new List<BookingActivityViewModel>();
    }

    public class BookingActivityViewModel
    {
        public DateTime Date { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Type { get; set; } // create, update, confirm, cancel, complete, payment
    }
}

