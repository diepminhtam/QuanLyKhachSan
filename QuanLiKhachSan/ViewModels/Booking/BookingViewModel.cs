using System.ComponentModel.DataAnnotations;

namespace QuanLiKhachSan.ViewModels.Booking
{
    public class BookingViewModel
    {
        public int RoomId { get; set; }

        public string RoomName { get; set; } = string.Empty;

        public string RoomType { get; set; } = string.Empty;

        public string RoomImageUrl { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng chọn ngày nhận phòng")]
        [Display(Name = "Ngày nhận phòng")]
        [DataType(DataType.Date)]
        public DateTime CheckInDate { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn ngày trả phòng")]
        [Display(Name = "Ngày trả phòng")]
        [DataType(DataType.Date)]
        public DateTime CheckOutDate { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn số khách")]
        [Range(1, 10, ErrorMessage = "Số khách phải từ 1 đến 10 người")]
        [Display(Name = "Số khách")]
        public int GuestCount { get; set; }

        [Display(Name = "Yêu cầu đặc biệt")]
        public string SpecialRequests { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập tên")]
        [Display(Name = "Tên")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập họ")]
        [Display(Name = "Họ")]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập địa chỉ email")]
        [EmailAddress(ErrorMessage = "Địa chỉ email không hợp lệ")]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        [Display(Name = "Số điện thoại")]
        public string Phone { get; set; } = string.Empty;

        // THÊM PROPERTY NÀY để khắc phục lỗi 'NumberOfNights'
        public int NumberOfNights => (CheckOutDate - CheckInDate).Days;

        // GIỮ NIGHTCOUNT để tương thích
        public int NightCount => NumberOfNights;

        public int MaxGuests { get; set; }

        public decimal RoomPrice { get; set; }

        public decimal Discount { get; set; }

        public decimal TotalPrice { get; set; }

        // SỬA LẠI TÍNH TOÁN TỔNG GIÁ - ĐƠN GIẢN HÓA
        public decimal CalculatedTotalPrice => (RoomPrice * NumberOfNights) - Discount;

        // THÊM PROPERTY ĐỂ BINDING TERMS
        [Required(ErrorMessage = "Vui lòng chấp nhận điều khoản")]
        [Range(typeof(bool), "true", "true", ErrorMessage = "Bạn phải đồng ý với điều khoản")]
        public bool AcceptTerms { get; set; }
    }
}