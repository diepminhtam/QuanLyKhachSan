using QuanLiKhachSan.Models; 
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace QuanLiKhachSan.Data
{
    public class SeedData
    {
        public static async Task InitializeAsync(IServiceProvider services)
        {
            var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
            var context = services.GetRequiredService<ApplicationDbContext>();
            var logger = services.GetRequiredService<ILogger<SeedData>>();

            try
            {
                // Đảm bảo database được tạo
                await context.Database.MigrateAsync();

                // Seed Roles
                await SeedRolesAsync(roleManager, logger);

                // Seed Admin User
                await SeedAdminUserAsync(userManager, logger);

                // Seed Status Data
                await SeedBookingStatusesAsync(context, logger);
                await SeedPaymentStatusesAsync(context, logger);

                // Cập nhật dữ liệu Rooms thành tiếng Việt
                await UpdateRoomsToVietnameseAsync(context, logger);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while seeding the database: {ErrorMessage}", ex.Message);
                throw;
            }
        }

        private static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager, ILogger logger)
        {
            logger.LogInformation("Checking if Admin role exists...");

            if (!await roleManager.RoleExistsAsync("Admin"))
            {
                logger.LogInformation("Creating Admin role...");
                var result = await roleManager.CreateAsync(new IdentityRole("Admin"));
                // ... (Xử lý lỗi) ...
            }

            // Tạo role Guest nếu cần
            if (!await roleManager.RoleExistsAsync("Guest"))
            {
                logger.LogInformation("Creating Guest role...");
                var result = await roleManager.CreateAsync(new IdentityRole("Guest"));
                // ... (Xử lý lỗi) ...
            }
        }

        private static async Task SeedAdminUserAsync(UserManager<ApplicationUser> userManager, ILogger logger)
        {
            logger.LogInformation("Checking if admin user exists...");
            var adminUser = await userManager.FindByEmailAsync("admin@hotel.com");

            if (adminUser == null)
            {
                logger.LogInformation("Creating admin user...");
                adminUser = new ApplicationUser
                {
                    UserName = "admin@hotel.com",
                    Email = "admin@hotel.com",
                    EmailConfirmed = true,
                    FullName = "Administrator"
                };

                var createResult = await userManager.CreateAsync(adminUser, "Admin123!");

                if (createResult.Succeeded)
                {
                    logger.LogInformation("Admin user created successfully.");
                    var roleResult = await userManager.AddToRoleAsync(adminUser, "Admin");
                    // ... (Xử lý lỗi) ...
                }
                // ... (Xử lý lỗi) ...
            }
        }

        // Tệp PDF gốc có một hàm SeedRoomsAsync, nhưng sau đó lại có
        // UpdateRoomsToVietnameseAsync. Tôi sẽ dùng hàm Update vì nó là hàm cuối cùng.
        private static async Task UpdateRoomsToVietnameseAsync(ApplicationDbContext context, ILogger logger)
        {
            logger.LogInformation("Updating rooms to Vietnamese data...");

            // Xóa tất cả rooms hiện tại
            var existingRooms = context.Rooms.ToList();
            if (existingRooms.Any())
            {
                context.Rooms.RemoveRange(existingRooms);
                await context.SaveChangesAsync();
                logger.LogInformation("Removed {Count} existing rooms.", existingRooms.Count);
            }

            // Thêm rooms tiếng Việt
            var vietnameseRooms = new List<Room>
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

            context.Rooms.AddRange(vietnameseRooms);
            await context.SaveChangesAsync();
            logger.LogInformation("Added {Count} Vietnamese rooms successfully.", vietnameseRooms.Count);
        }

        private static async Task SeedBookingStatusesAsync(ApplicationDbContext context, ILogger logger)
        {
            if (!context.BookingStatuses.Any())
            {
                logger.LogInformation("Seeding booking statuses...");
                var statuses = new List<BookingStatus>
                {
                    new BookingStatus { Name = "Chờ xác nhận", Description = "Đơn đặt phòng đang chờ xác nhận", IsActive = true },
                    new BookingStatus { Name = "Đã xác nhận", Description = "Đơn đặt phòng đã được xác nhận và thanh toán", IsActive = true },
                    new BookingStatus { Name = "Hoàn thành", Description = "Khách đã check-out", IsActive = true },
                    new BookingStatus { Name = "Đã hủy", Description = "Đơn đặt phòng đã bị hủy", IsActive = true }
                };
                context.BookingStatuses.AddRange(statuses);
                await context.SaveChangesAsync();
                logger.LogInformation("Booking statuses seeded successfully.");
            }
        }

        private static async Task SeedPaymentStatusesAsync(ApplicationDbContext context, ILogger logger)
        {
            if (!context.PaymentStatuses.Any())
            {
                logger.LogInformation("Seeding payment statuses...");
                var statuses = new List<PaymentStatus>
                {
                    new PaymentStatus { Name = "Đang xử lý", Description = "Thanh toán đang được xử lý", IsActive = true },
                    new PaymentStatus { Name = "Thành công", Description = "Thanh toán thành công", IsActive = true },
                    new PaymentStatus { Name = "Thất bại", Description = "Thanh toán thất bại", IsActive = true },
                    new PaymentStatus { Name = "Đã hoàn tiền", Description = "Đã hoàn tiền", IsActive = true }
                };
                context.PaymentStatuses.AddRange(statuses);
                await context.SaveChangesAsync();
                logger.LogInformation("Payment statuses seeded successfully.");
            }
        }
    }
}