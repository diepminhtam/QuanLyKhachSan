using Microsoft.EntityFrameworkCore;
using QuanLiKhachSan.Data;
using QuanLiKhachSan.Models;
using QuanLiKhachSan.Services.Interfaces;

namespace QuanLiKhachSan.Services.Implementations
{
    public class ReviewServiceImpl : IReviewService  // Đổi tên class
    {
        private readonly ApplicationDbContext _context;

        public ReviewServiceImpl(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<Review>> GetAllReviewsAsync()
        {
            return await _context.Reviews
                .Include(r => r.User)
                .Include(r => r.Room)
                .OrderByDescending(r => r.CreatedDate)
                .ToListAsync();
        }

        public async Task<Review?> GetReviewByIdAsync(int id)
        {
            return await _context.Reviews
                .Include(r => r.User)
                .Include(r => r.Room)
                .FirstOrDefaultAsync(r => r.Id == id);
        }

        public async Task<bool> UpdateReviewStatusAsync(int id, string status)
        {
            var review = await _context.Reviews.FindAsync(id);
            if (review == null) return false;

            review.Status = status;
            review.UpdatedDate = DateTime.Now;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteReviewAsync(int id)
        {
            var review = await _context.Reviews.FindAsync(id);
            if (review == null) return false;

            _context.Reviews.Remove(review);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<int> GetReviewsCountAsync()
        {
            return await _context.Reviews.CountAsync();
        }

        public async Task<int> GetGoodReviewsCountAsync()
        {
            return await _context.Reviews.CountAsync(r => r.Rating >= 4);
        }

        public async Task<int> GetAverageReviewsCountAsync()
        {
            return await _context.Reviews.CountAsync(r => r.Rating == 3);
        }

        public async Task<int> GetPoorReviewsCountAsync()
        {
            return await _context.Reviews.CountAsync(r => r.Rating <= 2);
        }
    }
}