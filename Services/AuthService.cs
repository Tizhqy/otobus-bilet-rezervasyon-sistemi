using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
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
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<AuthService> _logger;

        public AuthService(
            IUserRepository userRepository,
            IConfiguration configuration,
            IEmailService emailService,
            IHttpContextAccessor httpContextAccessor,
            ILogger<AuthService> logger)
        {
            _userRepository = userRepository;
            _configuration = configuration;
            _emailService = emailService;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        public async Task<AuthResponseDto> RegisterAsync(RegisterDto registerDto)
        {
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

            // Handle Remember Me
            if (loginDto.RememberMe)
            {
                var rememberToken = await GenerateRememberTokenAsync(user.Id);
                // The remember token should be returned separately or stored in a cookie by the controller
            }

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

        public async Task<string> GenerateRememberTokenAsync(int userId)
        {
            var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
            await _userRepository.UpdateRememberTokenAsync(userId, token);
            return token;
        }

        public async Task<AuthResponseDto> LoginWithRememberTokenAsync(string token)
        {
            var user = await _userRepository.GetByRememberTokenAsync(token);

            if (user == null)
            {
                return new AuthResponseDto
                {
                    Success = false,
                    Message = "Invalid or expired remember token."
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

            var jwtToken = await GenerateJwtTokenAsync(user.Id);

            return new AuthResponseDto
            {
                Success = true,
                Message = "Login successful.",
                Token = jwtToken,
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
        }

        public async Task RevokeRememberTokenAsync(int userId)
        {
            await _userRepository.UpdateRememberTokenAsync(userId, null);
        }

        public async Task<bool> RequestPasswordResetAsync(string email)
        {
            var user = await _userRepository.GetByEmailAsync(email);
            if (user == null) return false;

            var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

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
                _logger.LogWarning("Sifre sifirlama baglantisi uretilemedi. App:BaseUrl ayarini kontrol edin.");
                return false;
            }

            var mailSent = await _emailService.SendPasswordResetEmailAsync(user.Email, resetLink);
            if (!mailSent)
            {
                _logger.LogWarning("Sifre sifirlama e-postasi gonderilemedi. UserId={UserId}", user.Id);
                return false;
            }

            return true;
        }

        public async Task<bool> IsPasswordResetTokenValidAsync(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return false;

            var passwordReset = await _userRepository.GetPasswordResetByTokenAsync(token);
            return passwordReset != null;
        }

        public async Task<bool> ResetPasswordAsync(string token, string newPassword)
        {
            var passwordReset = await _userRepository.GetPasswordResetByTokenAsync(token);
            if (passwordReset == null) return false;

            var passwordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            await _userRepository.UpdatePasswordAsync(passwordReset.UserId, passwordHash);
            await _userRepository.MarkPasswordResetAsUsedAsync(passwordReset.Id);

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
    }
}
