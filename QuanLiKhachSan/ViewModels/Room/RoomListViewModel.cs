using QuanLiKhachSan.ViewModels.Room; 

namespace QuanLiKhachSan.ViewModels.Room 
{
    public class RoomListViewModel
    {
        public List<RoomCardViewModel> Rooms { get; set; } = new List<RoomCardViewModel>();

        // Filter & Search Parameters
        public DateTime? CheckInDate { get; set;}
        public DateTime? CheckOutDate { get; set;}
        public int Adults { get; set; } = 1;
        public int Children { get; set; } = 0;
        public string? RoomType { get; set; }
       
        public decimal? MinPrice { get; set; }
     
        public decimal? MaxPrice { get; set; }
    
        public string SortBy { get; set; } = "recommended"; 

        // Pagination
        public int CurrentPage { get; set; } = 1; 
        public int TotalPages { get; set; }
   
        public int PageSize { get; set; } = 6; 
        public int TotalRooms { get; set; }


        // Data for filter controls
        public List<string> RoomTypes { get; set; } = new List<string>(); 
        public decimal LowestPrice { get; set; }
     
        public decimal HighestPrice { get; set; }
   
    }
}