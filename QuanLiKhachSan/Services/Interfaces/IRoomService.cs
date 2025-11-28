
using QuanLiKhachSan.ViewModels.Room;

namespace QuanLiKhachSan.Services.Interfaces
{
    public interface IRoomService
    {
        // Dùng cho trang Home/Rooms (tìm kiếm, lọc)
        Task<RoomListViewModel> GetFilteredRoomsAsync(RoomListViewModel searchModel);

        // Dùng cho trang Rooms/Details
        Task<RoomDetailsViewModel> GetRoomDetailsAsync(int roomId);

        // Dùng cho trang Home/Index (Phòng nổi bật)
        Task<List<RoomCardViewModel>> GetFeaturedRoomsAsync(int count);
    }
}