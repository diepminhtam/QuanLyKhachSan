namespace QuanLiKhachSan.Models
{
    public class PaymentStatus
    {
        public int Id { get; set; }
        public string Name { get; set; } // Pending, Completed, Failed, Refunded
        public string Description { get; set; }
        public bool IsActive { get; set; } = true;

        public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
    }
}
