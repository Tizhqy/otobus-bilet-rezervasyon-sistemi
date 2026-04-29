using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using OtobusBiletRezervasyon.DTOs.Auth;
using OtobusBiletRezervasyon.Models;
using OtobusBiletRezervasyon.Repositories.Interfaces;
using OtobusBiletRezervasyon.Services.Interfaces;

namespace OtobusBiletRezervasyon.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly IConfiguration _configuration;
        private readonly IEmailService _emailService;
        private readonly ILogService _logService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<AuthService> _logger;

        public AuthService(
            IUserRepository userRepository,
            IConfiguration configuration,
            IEmailService emailService,
            ILogService logService,
            IHttpContextAccessor httpContextAccessor,
            ILogger<AuthService> logger)
        {
            _userRepository = userRepository;
            _configuration = configuration;
            _emailService = emailService;
            _logService = logService;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        public async Task<AuthResponseDto> RegisterAsync(RegisterDto registerDto)
        {
            if (!IsStrongPassword(registerDto.Password))
            {
                return new AuthResponseDto
                {
                    Success = false,
                    Message = $"Password must be at least {AppConfig.MinPasswordLength} characters and include uppercase, lowercase and numeric characters."
                };
            }

            // Check if email already exists
            if (await _userRepository.EmailExistsAsync(registerDto.Email))
            {
                return new AuthResponseDto
                {
                    Success = false,
                    Message = "Email already registered."
                };
            }

            // Get default user role
            var userRole = await _userRepository.GetRoleByNameAsync("user");
            if (userRole == null)
            {
                return new AuthResponseDto
                {
                    Success = false,
                    Message = "Default user role not found."
                };
            }

            // Create user
            var user = new User
            {
                FirstName = registerDto.FirstName,
                LastName = registerDto.LastName,
                Email = registerDto.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(registerDto.Password),
                Phone = registerDto.Phone,
                RoleId = userRole.Id,
                IsActive = true
            };

            await _userRepository.CreateAsync(user);

            // Generate JWT token
            var token = await GenerateJwtTokenAsync(user.Id);

            return new AuthResponseDto
            {
                Success = true,
                Message = "Registration successful.",
                Token = token,
                ExpiresAt = DateTime.UtcNow.AddHours(GetTokenExpirationHours()),
                User = new UserInfoDto
                {
                    Id = user.Id,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Email = user.Email,
                    Phone = user.Phone,
                    Role = userRole.Name
                }
            };
        }

        public async Task<AuthResponseDto> LoginAsync(LoginDto loginDto)
        {
            var user = await _userRepository.GetByEmailAsync(loginDto.Email);

            if (user == null || !BCrypt.Net.BCrypt.Verify(loginDto.Password, user.PasswordHash))
            {
                return new AuthResponseDto
                {
                    Success = false,
                    Message = "Invalid email or password."
                };
            }

            if (!user.IsActive)
            {
                return new AuthResponseDto
                {
                    Success = false,
                    Message = "Account is deactivated."
                };
            }

            var token = await GenerateJwtTokenAsync(user.Id);

            var response = new AuthResponseDto
            {
                Success = true,
                Message = "Login successful.",
                Token = token,
                ExpiresAt = DateTime.UtcNow.AddHours(GetTokenExpirationHours()),
                User = new UserInfoDto
                {
                    Id = user.Id,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Email = user.Email,
                    Phone = user.Phone,
                    Role = user.Role.Name
                }
            };

            // Remember-token login is intentionally not issued from this MVC flow.
            // Cookie persistence is controlled by AuthenticationProperties in AuthController.

            return response;
        }

        public async Task<string> GenerateJwtTokenAsync(int userId)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null) throw new ArgumentException("User not found");

            var jwtKey = _configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key not configured");
            var jwtIssuer = _configuration["Jwt:Issuer"] ?? "OtobusBiletRezervasyon";
            var jwtAudience = _configuration["Jwt:Audience"] ?? "OtobusBiletRezervasyon";

            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Name, $"{user.FirstName} {user.LastName}"),
                new Claim(ClaimTypes.Role, user.Role.Name),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var token = new JwtSecurityToken(
                issuer: jwtIssuer,
                audience: jwtAudience,
                claims: claims,
                expires: DateTime.UtcNow.AddHours(GetTokenExpirationHours()),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public Task<int?> ValidateJwtTokenAsync(string token)
        {
            try
            {
                var jwtKey = _configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key not configured");
                var jwtIssuer = _configuration["Jwt:Issuer"] ?? "OtobusBiletRezervasyon";
                var jwtAudience = _configuration["Jwt:Audience"] ?? "OtobusBiletRezervasyon";

                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.UTF8.GetBytes(jwtKey);

                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = jwtIssuer,
                    ValidateAudience = true,
                    ValidAudience = jwtAudience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };

                var principal = tokenHandler.ValidateToken(token, validationParameters, out _);
                var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier);

                if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
                {
                    return Task.FromResult<int?>(userId);
                }

                return Task.FromResult<int?>(null);
            }
            catch
            {
                return Task.FromResult<int?>(null);
            }
        }

        public async Task RevokeRememberTokenAsync(int userId)
        {
            await _userRepository.UpdateRememberTokenAsync(userId, null);
        }

        public async Task<bool> RequestPasswordResetAsync(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return true;

            var user = await _userRepository.GetByEmailAsync(email);
            if (user == null)
            {
                await Task.Delay(150);
                return true;
            }

            await _userRepository.MarkAllPasswordResetsAsUsedAsync(user.Id);

            var token = GeneratePasswordResetToken();

            var passwordReset = new PasswordReset
            {
                UserId = user.Id,
                Token = token,
                ExpiresAt = DateTime.UtcNow.AddHours(1),
                Used = false
            };

            await _userRepository.CreatePasswordResetAsync(passwordReset);

            var resetLink = BuildPasswordResetLink(token);
            if (string.IsNullOrWhiteSpace(resetLink))
            {
                await _userRepository.MarkPasswordResetAsUsedAsync(passwordReset.Id);
                _logger.LogWarning("Sifre sifirlama baglantisi uretilemedi. App:BaseUrl ayarini kontrol edin.");
                return true;
            }

            var mailSent = await _emailService.SendPasswordResetEmailAsync(user.Email, resetLink);
            if (!mailSent)
            {
                await _userRepository.MarkPasswordResetAsUsedAsync(passwordReset.Id);
                _logger.LogWarning("Sifre sifirlama e-postasi gonderilemedi. UserId={UserId}", user.Id);
                return true;
            }

            await _logService.LogPasswordResetRequestAsync(user.Id, GetClientIpAddress());
            return true;
        }

        public async Task<bool> IsPasswordResetTokenValidAsync(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return false;

            var normalizedToken = NormalizePasswordResetToken(token);
            var passwordReset = await _userRepository.GetPasswordResetByTokenAsync(normalizedToken);
            return passwordReset != null;
        }

        public async Task<bool> ResetPasswordAsync(string token, string newPassword)
        {
            if (string.IsNullOrWhiteSpace(token))
                return false;

            if (!IsStrongPassword(newPassword))
                return false;

            var normalizedToken = NormalizePasswordResetToken(token);
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            var userId = await _userRepository.ResetPasswordWithTokenAsync(normalizedToken, passwordHash);
            if (!userId.HasValue)
                return false;

            await _logService.LogPasswordChangeAsync(userId.Value, GetClientIpAddress());
            return true;
        }

        public async Task<UserInfoDto?> GetCurrentUserAsync(int userId)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null) return null;

            return new UserInfoDto
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                Phone = user.Phone,
                Role = user.Role.Name
            };
        }

        public async Task<bool> ChangePasswordAsync(int userId, string currentPassword, string newPassword)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null) return false;

            if (!IsStrongPassword(newPassword))
                return false;

            if (!BCrypt.Net.BCrypt.Verify(currentPassword, user.PasswordHash))
            {
                return false;
            }

            var newPasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            await _userRepository.UpdatePasswordAsync(userId, newPasswordHash);

            return true;
        }

        private int GetTokenExpirationHours()
        {
            var hours = _configuration["Jwt:ExpirationHours"];
            return int.TryParse(hours, out int result) ? result : 24;
        }

        private string? BuildPasswordResetLink(string token)
        {
            var baseUrl = _configuration["App:BaseUrl"]?.TrimEnd('/');

            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                var request = _httpContextAccessor.HttpContext?.Request;
                if (request != null)
                {
                    baseUrl = $"{request.Scheme}://{request.Host}";
                }
            }

            if (string.IsNullOrWhiteSpace(baseUrl))
                return null;

            return $"{baseUrl}/Auth/SifreSifirla?token={Uri.EscapeDataString(token)}";
        }

        private string GetClientIpAddress()
        {
            return _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        }

        private static string GeneratePasswordResetToken()
        {
            var tokenBytes = RandomNumberGenerator.GetBytes(32);
            return WebEncoders.Base64UrlEncode(tokenBytes);
        }

        private static string NormalizePasswordResetToken(string token)
        {
            var normalized = token.Trim();

            if (normalized.Contains('%'))
            {
                normalized = Uri.UnescapeDataString(normalized);
            }

            if (normalized.Contains(' '))
            {
                normalized = normalized.Replace(" ", "+", StringComparison.Ordinal);
            }

            return normalized;
        }

        private static bool IsStrongPassword(string? password)
        {
            if (string.IsNullOrWhiteSpace(password) || password.Length < AppConfig.MinPasswordLength)
                return false;

            return password.Any(char.IsUpper)
                && password.Any(char.IsLower)
                && password.Any(char.IsDigit);
        }
    }
}
