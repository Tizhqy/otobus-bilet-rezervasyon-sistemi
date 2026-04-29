using OtobusBiletRezervasyon.Models;
using OtobusBiletRezervasyon.Repositories.Interfaces;
using OtobusBiletRezervasyon.Services.Interfaces;

namespace OtobusBiletRezervasyon.Services
{
    public class CouponService : ICouponService
    {
        private readonly ICouponRepository _couponRepository;

        public CouponService(ICouponRepository couponRepository)
        {
            _couponRepository = couponRepository;
        }

        public async Task<Coupon?> GetValidCouponAsync(string code, int userId)
        {
            if (string.IsNullOrWhiteSpace(code)) return null;

            var coupon = await _couponRepository.GetByCodeAsync(code.Trim().ToUpperInvariant());
            if (coupon == null || !coupon.IsActive) return null;
            if (coupon.ValidUntil.HasValue && coupon.ValidUntil.Value < DateTime.UtcNow) return null;

            var isUsed = await _couponRepository.HasUserUsedCouponAsync(userId, coupon.Id);
            if (isUsed) return null;

            return coupon;
        }

        public async Task<decimal> CalculateDiscountAsync(string code, decimal originalPrice, int userId)
        {
            var coupon = await GetValidCouponAsync(code, userId);
            if (coupon == null) return originalPrice;

            if (coupon.DiscountType == "Percentage")
            {
                var discount = originalPrice * (coupon.DiscountAmount / 100m);
                var newPrice = originalPrice - discount;
                return newPrice < 0 ? 0 : newPrice;
            }
            else if (coupon.DiscountType == "Fixed")
            {
                var newPrice = originalPrice - coupon.DiscountAmount;
                return newPrice < 0 ? 0 : newPrice;
            }

            return originalPrice;
        }

        public async Task MarkCouponAsUsedAsync(int userId, string code)
        {
            if (string.IsNullOrWhiteSpace(code)) return;
            var coupon = await _couponRepository.GetByCodeAsync(code.Trim().ToUpperInvariant());
            if (coupon != null)
            {
                await _couponRepository.RecordCouponUsageAsync(userId, coupon.Id);
            }
        }
    }
}
