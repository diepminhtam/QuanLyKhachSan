namespace QuanLiKhachSan.Models
{
    public class Payment
    {
        public int Id { get; set; }
        public decimal Amount { get; set; }
        public DateTime PaymentDate { get; set; } = DateTime.Now;

        // Booking
        public int BookingId { get; set; }
        public virtual Booking Booking { get; set; } = null!;

        // PaymentStatus
        public int PaymentStatusId { get; set; }
        public virtual PaymentStatus PaymentStatus { get; set; } = null!;
    }
}