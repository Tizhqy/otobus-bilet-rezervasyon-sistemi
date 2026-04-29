using Microsoft.EntityFrameworkCore;
using OtobusBiletRezervasyon.Models;
using OtobusBiletRezervasyon.Repositories.Interfaces;

namespace OtobusBiletRezervasyon.Repositories
{
    public class CouponRepository : ICouponRepository
    {
        private readonly AppDbContext _context;

        public CouponRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Coupon?> GetByCodeAsync(string code)
        {
            return await _context.Coupons
                .FirstOrDefaultAsync(c => c.Code == code && c.IsActive);
        }

        public async Task<Coupon?> GetByIdAsync(int id)
        {
            return await _context.Coupons.FindAsync(id);
        }

        public async Task<IEnumerable<Coupon>> GetAllAsync()
        {
            return await _context.Coupons.OrderByDescending(c => c.CreatedAt).ToListAsync();
        }

        public async Task<Coupon> CreateAsync(Coupon coupon)
        {
            _context.Coupons.Add(coupon);
            await _context.SaveChangesAsync();
            return coupon;
        }

        public async Task<Coupon> UpdateAsync(Coupon coupon)
        {
            _context.Coupons.Update(coupon);
            await _context.SaveChangesAsync();
            return coupon;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var coupon = await _context.Coupons.FindAsync(id);
            if (coupon == null) return false;

            _context.Coupons.Remove(coupon);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> HasUserUsedCouponAsync(int userId, int couponId)
        {
            return await _context.CouponUsages
                .AnyAsync(cu => cu.UserId == userId && cu.CouponId == couponId);
        }

        public async Task RecordCouponUsageAsync(int userId, int couponId)
        {
            var alreadyUsed = await _context.CouponUsages
                .AnyAsync(cu => cu.UserId == userId && cu.CouponId == couponId);
            if (alreadyUsed)
                return;

            var usage = new CouponUsage
            {
                UserId = userId,
                CouponId = couponId,
                UsedAt = DateTime.UtcNow
            };
            
            _context.CouponUsages.Add(usage);
            await _context.SaveChangesAsync();
        }
    }
}
