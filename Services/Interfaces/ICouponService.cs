using OtobusBiletRezervasyon.Models;

namespace OtobusBiletRezervasyon.Services.Interfaces
{
    public interface ICouponService
    {
        Task<Coupon?> GetValidCouponAsync(string code, int userId);
        Task<decimal> CalculateDiscountAsync(string code, decimal originalPrice, int userId);
        Task MarkCouponAsUsedAsync(int userId, string code);
    }
}
