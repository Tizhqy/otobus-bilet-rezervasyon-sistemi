using OtobusBiletRezervasyon.Models;

namespace OtobusBiletRezervasyon.Repositories.Interfaces
{
    public interface ICouponRepository
    {
        Task<Coupon?> GetByCodeAsync(string code);
        Task<Coupon?> GetByIdAsync(int id);
        Task<IEnumerable<Coupon>> GetAllAsync();
        Task<Coupon> CreateAsync(Coupon coupon);
        Task<Coupon> UpdateAsync(Coupon coupon);
        Task<bool> DeleteAsync(int id);
        Task<bool> HasUserUsedCouponAsync(int userId, int couponId);
        Task RecordCouponUsageAsync(int userId, int couponId);
    }
}
