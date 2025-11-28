namespace QuanLiKhachSan.ViewModels
{
    public class ServiceViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string Duration { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public List<string> Features { get; set; } = new List<string>();

        public string PriceDisplay => Price == 0 ? "Miễn phí" : $"{Price.ToString("N0")} VNĐ";
        public bool IsFree => Price == 0;
    }
}