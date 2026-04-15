using OtobusBiletRezervasyon.DTOs.Auth;

namespace OtobusBiletRezervasyon.Services.Interfaces
{
    public interface IAuthService
    {
        // Registration
        Task<AuthResponseDto> RegisterAsync(RegisterDto registerDto);

        // Login
        Task<AuthResponseDto> LoginAsync(LoginDto loginDto);

        // JWT Token
        Task<string> GenerateJwtTokenAsync(int userId);
        Task<int?> ValidateJwtTokenAsync(string token);

        // Remember Me
        Task<string> GenerateRememberTokenAsync(int userId);
        Task<AuthResponseDto> LoginWithRememberTokenAsync(string token);
        Task RevokeRememberTokenAsync(int userId);

        // Password Reset
        Task<bool> RequestPasswordResetAsync(string email);
        Task<bool> IsPasswordResetTokenValidAsync(string token);
        Task<bool> ResetPasswordAsync(string token, string newPassword);

        // User Info
        Task<UserInfoDto?> GetCurrentUserAsync(int userId);
        Task<bool> ChangePasswordAsync(int userId, string currentPassword, string newPassword);
    }
}
