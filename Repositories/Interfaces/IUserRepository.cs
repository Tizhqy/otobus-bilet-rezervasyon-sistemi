using OtobusBiletRezervasyon.Models;

namespace OtobusBiletRezervasyon.Repositories.Interfaces
{
    public interface IUserRepository
    {
        Task<User?> GetByIdAsync(int id);
        Task<User?> GetByEmailAsync(string email);
        Task<IEnumerable<User>> GetAllAsync();
        Task<(IReadOnlyList<User> Users, int TotalCount)> GetPagedAsync(string? search, int page, int pageSize);
        Task<IEnumerable<User>> GetByRoleIdAsync(int roleId);
        Task<User> CreateAsync(User user);
        Task<User> UpdateAsync(User user);
        Task<bool> DeleteAsync(int id);
        Task<bool> ExistsAsync(int id);
        Task<bool> EmailExistsAsync(string email);
        Task UpdateRememberTokenAsync(int userId, string? token);
        Task UpdatePasswordAsync(int userId, string passwordHash);

        // Password Reset
        Task<PasswordReset> CreatePasswordResetAsync(PasswordReset passwordReset);
        Task<PasswordReset?> GetPasswordResetByTokenAsync(string token);
        Task MarkPasswordResetAsUsedAsync(int id);
        Task MarkAllPasswordResetsAsUsedAsync(int userId);
        Task<int?> ResetPasswordWithTokenAsync(string token, string passwordHash);

        // Role
        Task<Role?> GetRoleByIdAsync(int id);
        Task<Role?> GetRoleByNameAsync(string name);
    }
}
