using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using QuanLiKhachSan.Models;

namespace QuanLiKhachSan.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Room> Rooms { get; set; }
        public DbSet<Booking> Bookings { get; set; }
        public DbSet<BookingStatus> BookingStatuses { get; set; }
        public DbSet<BookingStatusHistory> BookingStatusHistories { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<PaymentStatus> PaymentStatuses { get; set; }
        public DbSet<Review> Reviews { get; set; }

        public DbSet<Service> Services { get; set; }


        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // --------------------------
            // Decimal configuration
            // --------------------------
            builder.Entity<Room>()
                .Property(r => r.PricePerNight)
                .HasColumnType("decimal(18, 2)");

            builder.Entity<Booking>()
                .Property(b => b.TotalPrice)
                .HasColumnType("decimal(18, 2)");

            builder.Entity<Payment>()
                .Property(p => p.Amount)
                .HasColumnType("decimal(18, 2)");

            // --------------------------
            // Relationships
            // --------------------------

            // Booking - Room (1 Room -> Many Bookings)
            builder.Entity<Booking>()
                .HasOne(b => b.Room)
                .WithMany(r => r.Bookings)
                .HasForeignKey(b => b.RoomId)
                .OnDelete(DeleteBehavior.Cascade);

            // Booking - User (1 User -> Many Bookings)
            builder.Entity<Booking>()
                .HasOne(b => b.User)
                .WithMany(u => u.Bookings)
                .HasForeignKey(b => b.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Booking - BookingStatus
            builder.Entity<Booking>()
                .HasOne(b => b.BookingStatus)
                .WithMany(bs => bs.Bookings)
                .HasForeignKey(b => b.BookingStatusId)
                .OnDelete(DeleteBehavior.Restrict);

            // Payment - Booking (1 Booking có nhiều Payment)
            builder.Entity<Payment>()
                .HasOne(p => p.Booking)
                .WithMany(b => b.Payments)
                .HasForeignKey(p => p.BookingId)
                .OnDelete(DeleteBehavior.Cascade); // vẫn cascade

            // Payment - PaymentStatus (1 Payment -> 1 PaymentStatus)
            builder.Entity<Payment>()
               .HasOne(p => p.PaymentStatus)
               .WithMany(ps => ps.Payments)
               .HasForeignKey(p => p.PaymentStatusId)
               .OnDelete(DeleteBehavior.Restrict);

            // Review - Booking (1-1)
            builder.Entity<Review>()
                .HasOne(r => r.Booking)
                .WithOne(b => b.Review)
                .HasForeignKey<Review>(r => r.BookingId)
                .OnDelete(DeleteBehavior.Restrict); // tránh multiple cascade paths

            // Review - User (1 User -> nhiều Reviews)
            builder.Entity<Review>()
                .HasOne(r => r.User)
                .WithMany(u => u.Reviews)
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Review - Room (1 Room -> nhiều Reviews)
            builder.Entity<Review>()
                .HasOne(r => r.Room)
                .WithMany(room => room.Reviews)
                .HasForeignKey(r => r.RoomId)
                .OnDelete(DeleteBehavior.Restrict);

            // BookingStatusHistory
            builder.Entity<BookingStatusHistory>()
                .HasOne(h => h.Booking)
                .WithMany() // Booking -> many histories
                .HasForeignKey(h => h.BookingId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<BookingStatusHistory>()
                .HasOne(h => h.FromBookingStatus)
                .WithMany()
                .HasForeignKey(h => h.FromBookingStatusId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<BookingStatusHistory>()
                .HasOne(h => h.ToBookingStatus)
                .WithMany()
                .HasForeignKey(h => h.ToBookingStatusId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
