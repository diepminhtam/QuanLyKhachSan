using QuanLiKhachSan.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace QuanLiKhachSan.Data
{
    public class SeedData
    {
        public static async Task InitializeAsync(IServiceProvider services, IConfiguration configuration)
        {
            var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
            var context = services.GetRequiredService<ApplicationDbContext>();
            var logger = services.GetRequiredService<ILogger<SeedData>>();

            try
            {
                // Đảm bảo DB cập nhật
                await context.Database.MigrateAsync();

                // Seed các thành phần cơ bản
                await SeedRolesAsync(roleManager, logger);
                await SeedAdminUserAsync(userManager, logger);
                await SeedBookingStatusesAsync(context, logger);
                await SeedPaymentStatusesAsync(context, logger);

                // Đọc biến FirstRun
                bool firstRun = configuration.GetValue<bool>("FirstRun");

                await UpdateRoomsToVietnameseAsync(context, logger, firstRun);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while seeding the database.");
                throw;
            }
        }

        private static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager, ILogger logger)
        {
            if (!await roleManager.RoleExistsAsync("Admin"))
                await roleManager.CreateAsync(new IdentityRole("Admin"));

            if (!await roleManager.RoleExistsAsync("Guest"))
                await roleManager.CreateAsync(new IdentityRole("Guest"));
        }

        private static async Task SeedAdminUserAsync(UserManager<ApplicationUser> userManager, ILogger logger)
        {
            var admin = await userManager.FindByEmailAsync("admin@hotel.com");

            if (admin == null)
            {
                admin = new ApplicationUser
                {
                    UserName = "admin@hotel.com",
                    Email = "admin@hotel.com",
                    FullName = "Administrator",
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(admin, "Admin123!");

                if (result.Succeeded)
                    await userManager.AddToRoleAsync(admin, "Admin");
            }
        }

        private static async Task UpdateRoomsToVietnameseAsync(ApplicationDbContext context, ILogger logger, bool firstRun)
        {
            if (!firstRun)
            {
                logger.LogInformation("FirstRun = false → Skip seeding Rooms.");
                return;
            }

            logger.LogInformation("FirstRun = true → Seeding Rooms...");

            // Xóa toàn bộ Rooms
            var existing = context.Rooms.ToList();
            if (existing.Any())
            {
                context.Rooms.RemoveRange(existing);
                await context.SaveChangesAsync();
            }

            // Thêm Rooms mới
            var rooms = new List<Room>
            {
                new Room
                {
                    Name = "Phòng Deluxe Hướng Biển",
                    Description = "Phòng sang trọng với tầm nhìn tuyệt đẹp ra biển và các tiện nghi hiện đại.",
                    PricePerNight = 2000000m,
                    ImageUrl = "https://movenpickresortcamranh.com/wp-content/uploads/2022/06/Movenpick-Resort-Cam-Ranh12.jpg",
                    Capacity = 2,
                    RoomType = "Deluxe",
                    IsAvailable = true,
                    AverageRating = 4.5
                },
                new Room
                {
                    Name = "Phòng Standard Giường Đôi",
                    Description = "Phòng thoải mái với giường đôi, phù hợp cho khách công tác.",
                    PricePerNight = 1200000m,
                    ImageUrl = "https://rosevalleydalat.com/wp-content/uploads/2019/05/Standard-double-1.jpg",
                    Capacity = 2,
                    RoomType = "Standard",
                    IsAvailable = true,
                    AverageRating = 4.0
                },
                new Room
                {
                    Name = "Phòng Suite Gia Đình",
                    Description = "Phòng rộng rãi lý tưởng cho gia đình có trẻ em.",
                    PricePerNight = 3500000m,
                    ImageUrl = "https://ezcloud.vn/wp-content/uploads/2023/10/family-suite-la-gi.webp",
                    Capacity = 4,
                    RoomType = "Suite",
                    IsAvailable = true,
                    AverageRating = 4.8
                },
                new Room
                {
                    Name = "Phòng Executive Business",
                    Description = "Phòng chuyên nghiệp với bàn làm việc và internet tốc độ cao.",
                    PricePerNight = 1800000m,
                    ImageUrl = "https://ezcloud.vn/wp-content/uploads/2023/10/phong-executive-la-gi.webp",
                    Capacity = 1,
                    RoomType = "Executive",
                    IsAvailable = true,
                    AverageRating = 4.3
                },
                new Room
                {
                    Name = "Phòng Tổng Thống",
                    Description = "Phòng cao cấp nhất với ban công riêng và dịch vụ premium.",
                    PricePerNight = 8000000m,
                    ImageUrl = "https://images2.thanhnien.vn/528068263637045248/2023/9/11/biden-16-169441443748282765858.jpg",
                    Capacity = 2,
                    RoomType = "Presidential",
                    IsAvailable = true,
                    AverageRating = 5.0
                }
            };

            context.Rooms.AddRange(rooms);
            await context.SaveChangesAsync();
            logger.LogInformation("Seeding Rooms completed.");
        }

        private static async Task SeedBookingStatusesAsync(ApplicationDbContext context, ILogger logger)
        {
            if (!context.BookingStatuses.Any())
            {
                context.BookingStatuses.AddRange(
                    new BookingStatus { Name = "Chờ xác nhận", IsActive = true },
                    new BookingStatus { Name = "Đã xác nhận", IsActive = true },
                    new BookingStatus { Name = "Hoàn thành", IsActive = true },
                    new BookingStatus { Name = "Đã hủy", IsActive = true }
                );
                await context.SaveChangesAsync();
            }
        }

        private static async Task SeedPaymentStatusesAsync(ApplicationDbContext context, ILogger logger)
        {
            if (!context.PaymentStatuses.Any())
            {
                context.PaymentStatuses.AddRange(
                    new PaymentStatus { Name = "Đang xử lý", IsActive = true },
                    new PaymentStatus { Name = "Thành công", IsActive = true },
                    new PaymentStatus { Name = "Thất bại", IsActive = true },
                    new PaymentStatus { Name = "Đã hoàn tiền", IsActive = true }
                );
                await context.SaveChangesAsync();
            }
        }
    }
}
