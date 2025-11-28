using System.ComponentModel.DataAnnotations;

namespace QuanLiKhachSan.ViewModels.Booking
{
    public class BookingConfirmationViewModel
    {
        public int BookingId { get; set; }

        [Display(Name = "Mã đặt phòng")]
        public string BookingNumber { get; set; } = string.Empty;

        [Display(Name = "Tên khách hàng")]
        public string CustomerName { get; set; } = string.Empty;

        [Display(Name = "Tên phòng")]
        public string RoomName { get; set; } = string.Empty;

        [Display(Name = "Ngày nhận phòng")]
        [DataType(DataType.Date)]
        public DateTime CheckInDate { get; set; }

        [Display(Name = "Ngày trả phòng")]
        [DataType(DataType.Date)]
        public DateTime CheckOutDate { get; set; }

        [Display(Name = "Số khách")]
        public int GuestCount { get; set; }

        [Display(Name = "Tổng tiền")]
        [DataType(DataType.Currency)]
        public decimal TotalPrice { get; set; }

        [Display(Name = "Trạng thái")]
        public string Status { get; set; } = string.Empty;

        [Display(Name = "Ngày đặt")]
        public DateTime BookingDate { get; set; }
    }
}