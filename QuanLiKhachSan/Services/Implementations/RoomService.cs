
using QuanLiKhachSan.Data;
using QuanLiKhachSan.Services.Interfaces;
using QuanLiKhachSan.ViewModels.Room;
using Microsoft.EntityFrameworkCore;

namespace QuanLiKhachSan.Services.Implementations
{
    public class RoomService : IRoomService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<RoomService> _logger;

        public RoomService(ApplicationDbContext context, ILogger<RoomService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<RoomListViewModel> GetFilteredRoomsAsync(RoomListViewModel searchModel)
        {
            
            var query = _context.Rooms.AsQueryable();

            // Filtering logic
            int totalGuests = searchModel.Adults + searchModel.Children;
            if (totalGuests > 1) query = query.Where(r => r.Capacity >= totalGuests);
            if (!string.IsNullOrEmpty(searchModel.RoomType)) query = query.Where(r => r.RoomType == searchModel.RoomType);
            if (searchModel.MinPrice.HasValue) query = query.Where(r => r.PricePerNight >= searchModel.MinPrice.Value);
            if (searchModel.MaxPrice.HasValue) query = query.Where(r => r.PricePerNight <= searchModel.MaxPrice.Value);

            // Sorting logic (ĐÃ THAY ĐỔI: Bỏ sắp xếp theo 'rating')
            switch (searchModel.SortBy?.ToLower())
            {
                case "price-low":
                    query = query.OrderBy(r => r.PricePerNight);
                    break;
                case "price-high":
                    query = query.OrderByDescending(r => r.PricePerNight);
                    break;
                default:
                    // Sắp xếp mặc định mới (theo giá thấp)
                    query = query.OrderBy(r => r.PricePerNight);
                    break;
            }

            var allMatchingRooms = await query.ToListAsync();
            var totalRooms = allMatchingRooms.Count;

            // Determine date range for availability check
            DateTime? checkIn = searchModel.CheckInDate;
            DateTime? checkOut = searchModel.CheckOutDate;

            // Get cancelled status id to exclude cancelled bookings
            var cancelledStatus = await _context.BookingStatuses.AsNoTracking().FirstOrDefaultAsync(bs => bs.Name == "Đã hủy");
            var cancelledId = cancelledStatus?.Id ?? -1;

            // Load bookings that overlap the requested range (or current date if no range provided)
            List<Models.Booking> overlappingBookings = new();
            if (checkIn.HasValue && checkOut.HasValue)
            {
                overlappingBookings = await _context.Bookings
                    .AsNoTracking()
                    .Where(b => b.BookingStatusId != cancelledId && b.CheckIn < checkOut.Value && b.CheckOut > checkIn.Value)
                    .ToListAsync();
            }
            else
            {
                // If no date range provided, consider current occupancy
                var now = DateTime.Now;
                overlappingBookings = await _context.Bookings
                    .AsNoTracking()
                    .Where(b => b.BookingStatusId != cancelledId && b.CheckIn <= now && b.CheckOut >= now)
                    .ToListAsync();
            }

            var roomsForPage = allMatchingRooms
                .Skip((searchModel.CurrentPage - 1) * searchModel.PageSize)
                .Take(searchModel.PageSize)
                .Select(room => new RoomCardViewModel
                {
                    Id = room.Id,
                    Name = room.Name,
                    Description = room.Description,
                    ImageUrl = room.ImageUrl,
                    RoomType = room.RoomType,
                    Capacity = room.Capacity,
                    PricePerNight = room.PricePerNight,
                    AverageRating = room.AverageRating,
                    ReviewsCount = room.Reviews?.Count ?? 0,
                    Amenities = new List<string> { "WiFi", "TV", "Điều hòa" },
                    IsAvailable = !(checkIn.HasValue && checkOut.HasValue)
                        ? !overlappingBookings.Any(b => b.RoomId == room.Id)
                        : !overlappingBookings.Any(b => b.RoomId == room.Id)
                }).ToList();

            var viewModel = new RoomListViewModel
            {
                Rooms = roomsForPage,
                CurrentPage = searchModel.CurrentPage,
                TotalRooms = totalRooms,
                PageSize = searchModel.PageSize,
                TotalPages = (int)Math.Ceiling(totalRooms / (double)searchModel.PageSize),
                RoomTypes = await _context.Rooms.Select(r => r.RoomType).Distinct().ToListAsync(),
                LowestPrice = allMatchingRooms.Any() ? allMatchingRooms.Min(r => r.PricePerNight) : 0,
                HighestPrice = allMatchingRooms.Any() ? allMatchingRooms.Max(r => r.PricePerNight) : 0
            };

           
            viewModel.CheckInDate = searchModel.CheckInDate;
            viewModel.CheckOutDate = searchModel.CheckOutDate;
            viewModel.Adults = searchModel.Adults;
            viewModel.Children = searchModel.Children;
            viewModel.RoomType = searchModel.RoomType;
            viewModel.MinPrice = searchModel.MinPrice ?? viewModel.LowestPrice;
            viewModel.MaxPrice = searchModel.MaxPrice ?? viewModel.HighestPrice;
            viewModel.SortBy = searchModel.SortBy;

            return viewModel;
        }

        public async Task<RoomDetailsViewModel> GetRoomDetailsAsync(int roomId)
        {
            var room = await _context.Rooms
                .Include(r => r.Reviews) // ĐẢM BẢO CÓ INCLUDE REVIEWS
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.Id == roomId);

            if (room == null)
            {
                throw new InvalidOperationException($"Room with ID {roomId} not found.");
            }

            // Lấy các phòng tương tự
            var similarRooms = await _context.Rooms
                .Where(r => r.RoomType == room.RoomType && r.Id != roomId)
                .Take(4)
                .Select(r => new SimilarRoomViewModel
                {
                    Id = r.Id,
                    Name = r.Name,
                    ImageUrl = r.ImageUrl,
                    Price = r.PricePerNight,
                    Rating = r.AverageRating
                })
                .ToListAsync();

            var viewModel = new RoomDetailsViewModel
            {
                Id = room.Id,
                Name = room.Name,
                Description = room.Description,
                MainImageUrl = room.ImageUrl,
                ImageUrls = new List<string> { room.ImageUrl },
                RoomType = room.RoomType,
                Capacity = room.Capacity,
                PricePerNight = room.PricePerNight,
                AverageRating = room.AverageRating,
                ReviewsCount = room.Reviews?.Count ?? 0,
                Reviews = new List<ReviewViewModel>(), 
                SimilarRooms = new List<SimilarRoomViewModel>(), 
                Features = new List<RoomFeatureViewModel>
    {
        new() { Name = "WiFi miễn phí", Icon = "fas fa-wifi" },
        new() { Name = "Điều hòa", Icon = "fas fa-snowflake" },
        new() { Name = "TV màn hình phẳng", Icon = "fas fa-tv" },
    }
            };

            return viewModel;
        }

        public async Task<List<RoomCardViewModel>> GetFeaturedRoomsAsync(int count)
        {
            
            return await _context.Rooms
                .OrderBy(r => r.Id) 
                .Take(count)
                .Select(r => new RoomCardViewModel
                {
                    Id = r.Id,
                    Name = r.Name,
                    Description = r.Description,
                    ImageUrl = r.ImageUrl,
                    RoomType = r.RoomType,
                    Capacity = r.Capacity,
                    PricePerNight = r.PricePerNight
                   
                })
                .ToListAsync();
        }

    }
}