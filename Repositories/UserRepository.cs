using Microsoft.EntityFrameworkCore;
using OtobusBiletRezervasyon.Models;
using OtobusBiletRezervasyon.Repositories.Interfaces;

namespace OtobusBiletRezervasyon.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly AppDbContext _context;

        public UserRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<User?> GetByIdAsync(int id)
        {
            return await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Id == id);
        }

        public async Task<User?> GetByEmailAsync(string email)
        {
            return await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<IEnumerable<User>> GetAllAsync()
        {
            return await _context.Users
                .Include(u => u.Role)
                .ToListAsync();
        }

        public async Task<(IReadOnlyList<User> Users, int TotalCount)> GetPagedAsync(string? search, int page, int pageSize)
        {
            var safePage = page < 1 ? 1 : page;
            var safePageSize = pageSize < 1 ? 20 : pageSize;

            IQueryable<User> query = _context.Users
                .AsNoTracking()
                .Include(u => u.Role);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var normalizedSearch = search.Trim();
                var likePattern = $"%{normalizedSearch}%";
                query = query.Where(u =>
                    EF.Functions.Like(u.FirstName, likePattern) ||
                    EF.Functions.Like(u.LastName, likePattern) ||
                    EF.Functions.Like(u.Email, likePattern));
            }

            var totalCount = await query.CountAsync();
            var users = await query
                .OrderByDescending(u => u.CreatedAt)
                .ThenByDescending(u => u.Id)
                .Skip((safePage - 1) * safePageSize)
                .Take(safePageSize)
                .ToListAsync();

            return (users, totalCount);
        }

        public async Task<IEnumerable<User>> GetByRoleIdAsync(int roleId)
        {
            return await _context.Users
                .Include(u => u.Role)
                .Where(u => u.RoleId == roleId)
                .ToListAsync();
        }

        public async Task<User> CreateAsync(User user)
        {
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return user;
        }

        public async Task<User> UpdateAsync(User user)
        {
            user.UpdatedAt = DateTime.UtcNow;
            _context.Users.Update(user);
            await _context.SaveChangesAsync();
            return user;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return false;

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ExistsAsync(int id)
        {
            return await _context.Users.AnyAsync(u => u.Id == id);
        }

        public async Task<bool> EmailExistsAsync(string email)
        {
            return await _context.Users.AnyAsync(u => u.Email == email);
        }

        public async Task UpdateRememberTokenAsync(int userId, string? token)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user != null)
            {
                user.RememberToken = token;
                user.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }

        public async Task UpdatePasswordAsync(int userId, string passwordHash)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user != null)
            {
                user.PasswordHash = passwordHash;
                user.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }

        // Password Reset
        public async Task<PasswordReset> CreatePasswordResetAsync(PasswordReset passwordReset)
        {
            _context.PasswordResets.Add(passwordReset);
            await _context.SaveChangesAsync();
            return passwordReset;
        }

        public async Task<PasswordReset?> GetPasswordResetByTokenAsync(string token)
        {
            return await _context.PasswordResets
                .FirstOrDefaultAsync(pr => pr.Token == token && !pr.Used && pr.ExpiresAt > DateTime.UtcNow);
        }

        public async Task MarkPasswordResetAsUsedAsync(int id)
        {
            var passwordReset = await _context.PasswordResets.FindAsync(id);
            if (passwordReset != null)
            {
                passwordReset.Used = true;
                await _context.SaveChangesAsync();
            }
        }

        public async Task MarkAllPasswordResetsAsUsedAsync(int userId)
        {
            var resets = await _context.PasswordResets
                .Where(pr => pr.UserId == userId && !pr.Used)
                .ToListAsync();

            if (!resets.Any())
                return;

            foreach (var reset in resets)
            {
                reset.Used = true;
            }

            await _context.SaveChangesAsync();
        }

        public async Task<int?> ResetPasswordWithTokenAsync(string token, string passwordHash)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();

            var now = DateTime.UtcNow;
            var passwordReset = await _context.PasswordResets
                .FirstOrDefaultAsync(pr => pr.Token == token && !pr.Used && pr.ExpiresAt > now);

            if (passwordReset == null)
                return null;

            passwordReset.Used = true;

            var user = await _context.Users.FindAsync(passwordReset.UserId);
            if (user == null)
            {
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return null;
            }

            user.PasswordHash = passwordHash;
            user.UpdatedAt = now;

            var otherResets = await _context.PasswordResets
                .Where(pr => pr.UserId == passwordReset.UserId && !pr.Used && pr.Id != passwordReset.Id)
                .ToListAsync();

            foreach (var reset in otherResets)
            {
                reset.Used = true;
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            return passwordReset.UserId;
        }

        // Role
        public async Task<Role?> GetRoleByIdAsync(int id)
        {
            return await _context.Roles.FindAsync(id);
        }

        public async Task<Role?> GetRoleByNameAsync(string name)
        {
            return await _context.Roles.FirstOrDefaultAsync(r => r.Name == name);
        }
    }
}
