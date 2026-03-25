using OtobusBiletRezervasyon.Models;

namespace OtobusBiletRezervasyon.Repositories.Interfaces
{
    public interface IUserRepository
    {
        Task<User?> GetByIdAsync(int id);
        Task<User?> GetByEmailAsync(string email);
        Task<User?> GetByRememberTokenAsync(string token);
        Task<IEnumerable<User>> GetAllAsync();
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

        // Role
        Task<Role?> GetRoleByIdAsync(int id);
        Task<Role?> GetRoleByNameAsync(string name);
    }
}
