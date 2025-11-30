using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace QuanLiKhachSan.ViewModels.Booking
{
    // BookingViewModel implements IValidatableObject for cross-field validation
}

namespace QuanLiKhachSan.ViewModels.Booking
{
    public class BookingViewModel : IValidatableObject
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

        // Flag to indicate availability for the selected dates
        public bool IsAvailable { get; set; } = true;

        // SỬA LẠI TÍNH TOÁN TỔNG GIÁ - ĐƠN GIẢN HÓA
        public decimal CalculatedTotalPrice => (RoomPrice * NumberOfNights) - Discount;

        // Property để binding AcceptTerms
        public bool AcceptTerms { get; set; }

        // Cross-field validation
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();

            // Check dates
            if (CheckInDate == default)
            {
                results.Add(new ValidationResult("Vui lòng chọn ngày nhận phòng", new[] { nameof(CheckInDate) }));
            }

            if (CheckOutDate == default)
            {
                results.Add(new ValidationResult("Vui lòng chọn ngày trả phòng", new[] { nameof(CheckOutDate) }));
            }

            if (CheckInDate != default && CheckOutDate != default && CheckOutDate <= CheckInDate)
            {
                results.Add(new ValidationResult("Ngày trả phòng phải sau ngày nhận phòng", new[] { nameof(CheckOutDate) }));
            }

            // Guest count vs capacity
            if (MaxGuests > 0 && GuestCount > MaxGuests)
            {
                results.Add(new ValidationResult($"Số khách không được vượt quá {MaxGuests} người", new[] { nameof(GuestCount) }));
            }

            // Accept terms must be checked
            if (!AcceptTerms)
            {
                results.Add(new ValidationResult("Bạn phải đồng ý với Điều khoản và Điều kiện", new[] { nameof(AcceptTerms) }));
            }

            return results;
        }
    }
}