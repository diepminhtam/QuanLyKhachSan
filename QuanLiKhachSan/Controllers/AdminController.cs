
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLiKhachSan.Data;
using QuanLiKhachSan.Models;
using QuanLiKhachSan.ViewModels.Admin;
using System.Globalization;


namespace QuanLiKhachSan.Controllers
{
    [Route("Admin")]
    public class AdminController : Controller
    {
            [HttpGet("Dashboard")]
            public IActionResult Dashboard()
        {
            // Calculate metrics
            var totalRevenue = _context.Bookings.Sum(b => b.TotalPrice);
            var totalBookings = _context.Bookings.Count();
            var totalRooms = _context.Rooms.Count();
            var occupiedRooms = _context.Bookings.Count(b => b.CheckIn <= DateTime.Now && b.CheckOut >= DateTime.Now);
            var occupancyRate = totalRooms > 0 ? (double)occupiedRooms / totalRooms * 100 : 0;
            var totalCustomers = _context.Users.Count();

            var dashboardVM = new DashboardViewModel
            {
                TotalRevenue = totalRevenue,
                TotalBookings = totalBookings,
                RoomOccupancyRate = occupancyRate,
                TotalCustomers = totalCustomers
            };

            return View("Dashboard", dashboardVM);
        }

            private readonly ApplicationDbContext _context;

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("Bookings")]
        public async Task<IActionResult> Bookings()
        {
            // Lấy dữ liệu thực từ database
            var bookings = await _context.Bookings
                .Include(b => b.User)
                .Include(b => b.Room)
                .Include(b => b.BookingStatus)
                .ToListAsync();

            var viewModel = new BookingsViewModel
            {
                Bookings = bookings.Select(b => new BookingItemViewModel
                {
                    Id = b.Id,
                    BookingNumber = $"BK{b.Id:D6}",
                    CustomerName = b.User.FullName,
                    CustomerEmail = b.User.Email, // THÊM EMAIL
                    RoomName = b.Room.Name,
                    CheckInDate = b.CheckIn,
                    CheckOutDate = b.CheckOut,
                    GuestCount = b.Guests, // SỬA THÀNH GuestCount
                    TotalPrice = b.TotalPrice,
                    Status = b.BookingStatus.Name,
                    PaymentStatus = "Chưa thanh toán", // TẠM THỜI
                    BookingDate = b.CreatedDate
                }).ToList()
            };

            return View("AdminBookings", viewModel);
        }

        [HttpGet("Rooms")]
        public IActionResult Rooms()
        {
            var rooms = _context.Rooms.ToList();
            var now = DateTime.Now;
            var occupiedRoomIds = _context.Bookings
                .Where(b => b.CheckIn <= now && b.CheckOut >= now)
                .Select(b => b.RoomId)
                .ToHashSet();

            var viewModel = new RoomsViewModel
            {
                Rooms = rooms.Select(r => new RoomItemViewModel
                {
                    Id = r.Id,
                    Name = r.Name,
                    RoomType = r.RoomType,
                    PricePerNight = r.PricePerNight,
                    Capacity = r.Capacity,
                    Status = occupiedRoomIds.Contains(r.Id) ? "Đang sử dụng" : (r.IsAvailable ? "Trống" : "Bảo trì"),
                    Floor = 0, // Add if you have this info
                    RoomNumber = r.Id.ToString(), // Add if you have this info
                    Rating = r.AverageRating,
                    ImageUrl = r.ImageUrl,
                    Description = r.Description,
                    Amenities = new List<string>() // Add if you have this info
                }).ToList(),
                TotalRooms = rooms.Count,
                AvailableCount = rooms.Count(r => r.IsAvailable && !occupiedRoomIds.Contains(r.Id)),
                OccupiedCount = rooms.Count(r => occupiedRoomIds.Contains(r.Id)),
                MaintenanceCount = rooms.Count(r => !r.IsAvailable),
                CleaningCount = 0 // Add logic if you track cleaning status
            };
            return View("AdminRooms", viewModel);
        }

        [HttpGet("Users")]
        public IActionResult Users()
        {
            var viewModel = new UsersViewModel
            {
                Users = GetSampleUsers()
            };
            return View("AdminUsers", viewModel);
        }

        private List<UserItemViewModel> GetSampleUsers()
        {

            return new List<UserItemViewModel>();
        }


        [HttpGet("Promotions")]
        public IActionResult Promotions()
        {
            return View("AdminPromotions");
        }

        [HttpGet("Reports")]
        public IActionResult Reports()
        {
            return View("AdminReports");
        }

        [HttpGet("AddRoom")]
        public IActionResult AddRoom()
        {
            return View();
        }

        [HttpPost("AddRoom")]
        public async Task<IActionResult> AddRoom(RoomCreateViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Xử lý upload hình ảnh (giả lập)
                    string mainImagePath = "/images/rooms/room-placeholder.jpg";
                    if (model.MainImage != null)
                    {
                        // Giả lập đường dẫn ảnh thành công
                        mainImagePath = $"/images/rooms/{Guid.NewGuid()}.jpg";
                    }

                    // Giả lập thành công và chuyển hướng
                    TempData["SuccessMessage"] = "Thêm phòng mới thành công!";
                    return RedirectToAction("Rooms");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Có lỗi khi thêm phòng: {ex.Message}");
                }
            }
            return View(model);
        }

        public IActionResult Reviews()
        {
            // Tạo dữ liệu mẫu trực tiếp
            var reviews = new List<ReviewViewModel>
            {
                new ReviewViewModel
                {
                    Id = 1,
                    CustomerName = "Nguyễn Văn A",
                    CustomerEmail = "nguyena@email.com",
                    RoomName = "Deluxe Room",
                    RoomType = "Deluxe",
                    Rating = 5,
                    Title = "Trải nghiệm tuyệt vời!",
                    Comment = "Phòng rất sạch sẽ, nhân viên thân thiện. Tôi rất hài lòng!",
                    Status = "Approved",
                    CreatedDate = DateTime.Now.AddDays(-2)
                },
                new ReviewViewModel
                {
                    Id = 2,
                    CustomerName = "Trần Thị B",
                    CustomerEmail = "tranb@email.com",
                    RoomName = "Superior Room",
                    RoomType = "Superior",
                    Rating = 4,
                    Title = "Tốt nhưng cần cải thiện",
                    Comment = "Phòng đẹp nhưng wifi hơi chậm. Nhìn chung là tốt.",
                    Status = "Pending",
                    CreatedDate = DateTime.Now.AddDays(-1)
                },
                new ReviewViewModel
                {
                    Id = 3,
                    CustomerName = "Lê Văn C",
                    CustomerEmail = "lec@email.com",
                    RoomName = "Executive Suite",
                    RoomType = "Suite",
                    Rating = 3,
                    Title = "Bình thường",
                    Comment = "Phòng ổn nhưng không có gì đặc biệt. Giá hơi cao.",
                    Status = "Pending",
                    CreatedDate = DateTime.Now
                },
                new ReviewViewModel
                {
                    Id = 4,
                    CustomerName = "Phạm Thị D",
                    CustomerEmail = "phamd@email.com",
                    RoomName = "Family Room",
                    RoomType = "Family",
                    Rating = 5,
                    Title = "Hoàn hảo cho gia đình",
                    Comment = "Phòng rộng rãi, view đẹp. Dịch vụ rất tốt!",
                    Status = "Approved",
                    CreatedDate = DateTime.Now.AddDays(-3)
                },
                new ReviewViewModel
                {
                    Id = 5,
                    CustomerName = "Hoàng Văn E",
                    CustomerEmail = "hoange@email.com",
                    RoomName = "Standard Room",
                    RoomType = "Standard",
                    Rating = 2,
                    Title = "Không như mong đợi",
                    Comment = "Phòng nhỏ, thiết bị cũ. Cần cải thiện nhiều.",
                    Status = "Rejected",
                    CreatedDate = DateTime.Now.AddDays(-5)
                }
            };

            return View(reviews);
        }

        [HttpPost("ConfirmBooking/{id}")]
        public async Task<IActionResult> ConfirmBooking(int id)
        {
            try
            {
                var booking = await _context.Bookings
                    .Include(b => b.BookingStatus)
                    .FirstOrDefaultAsync(b => b.Id == id);

                if (booking == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy đơn đặt phòng" });
                }

                var confirmedStatus = await _context.BookingStatuses
                    .FirstOrDefaultAsync(s => s.Name == "Đã xác nhận");

                if (confirmedStatus == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy trạng thái xác nhận" });
                }

                booking.BookingStatusId = confirmedStatus.Id;
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Đã xác nhận đơn đặt phòng thành công" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Lỗi: {ex.Message}" });
            }
        }

        [HttpPost("CancelBooking/{id}")]
        public async Task<IActionResult> CancelBooking(int id)
        {
            try
            {
                var booking = await _context.Bookings
                    .Include(b => b.BookingStatus)
                    .FirstOrDefaultAsync(b => b.Id == id);

                if (booking == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy đơn đặt phòng" });
                }

                var cancelledStatus = await _context.BookingStatuses
                    .FirstOrDefaultAsync(s => s.Name == "Đã hủy");

                if (cancelledStatus == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy trạng thái hủy" });
                }

                booking.BookingStatusId = cancelledStatus.Id;
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Đã hủy đơn đặt phòng thành công" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Lỗi: {ex.Message}" });
            }
        }

    }
}