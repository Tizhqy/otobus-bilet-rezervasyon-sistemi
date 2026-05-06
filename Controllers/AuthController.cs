using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using System.Security.Claims;
using OtobusBiletRezervasyon.DTOs.Auth;
using OtobusBiletRezervasyon.Services.Interfaces;

namespace OtobusBiletRezervasyon.Controllers
{
    public class AuthController : Controller
    {
        private const int MaxFailedLoginAttempts = 5;
        private static readonly TimeSpan FailedAttemptWindow = TimeSpan.FromMinutes(15);
        private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

        private readonly IAuthService _authService;
        private readonly ILogService _logService;
        private readonly IMemoryCache _memoryCache;

        public AuthController(IAuthService authService, ILogService logService, IMemoryCache memoryCache)
        {
            _authService = authService;
            _logService = logService;
            _memoryCache = memoryCache;
        }

        #region Kayit (Register)

        [HttpGet]
        public IActionResult Kayit()
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Index", "Sefer");

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Kayit(RegisterDto model)
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Index", "Sefer");

            if (!ModelState.IsValid)
                return View(model);

            var result = await _authService.RegisterAsync(model);

            if (!result.Success)
            {
                ModelState.AddModelError(string.Empty, result.Message);
                return View(model);
            }

            await _logService.LogRegistrationAsync(result.User!.Id, GetClientIpAddress());

            TempData["Basari"] = "Kayit basarili. Giris yapabilirsiniz.";
            return RedirectToAction("Giris");
        }

        #endregion

        #region Giris (Login)

        [HttpGet]
        public IActionResult Giris(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Index", "Sefer");

            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("login")]
        public async Task<IActionResult> Giris(LoginDto model, string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Index", "Sefer");

            var normalizedEmail = NormalizeEmail(model.Email);
            if (TryGetLockoutRemaining(normalizedEmail, out var remaining))
            {
                ModelState.AddModelError(string.Empty,
                    $"Cok fazla basarisiz giris denemesi. Lutfen {Math.Ceiling(remaining.TotalMinutes)} dakika sonra tekrar deneyin.");
                ViewBag.ReturnUrl = returnUrl;
                return View(model);
            }

            if (!ModelState.IsValid)
            {
                ViewBag.ReturnUrl = returnUrl;
                return View(model);
            }

            var result = await _authService.LoginAsync(model);

            if (!result.Success)
            {
                RegisterFailedAttempt(normalizedEmail);
                ModelState.AddModelError(string.Empty, result.Message);
                ViewBag.ReturnUrl = returnUrl;
                return View(model);
            }

            ResetLoginProtection(normalizedEmail);

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, result.User!.Id.ToString()),
                new(ClaimTypes.Name, $"{result.User.FirstName} {result.User.LastName}"),
                new(ClaimTypes.Email, result.User.Email),
                new(ClaimTypes.Role, result.User.Role)
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = model.RememberMe,
                ExpiresUtc = model.RememberMe
                    ? DateTimeOffset.UtcNow.AddDays(14)
                    : DateTimeOffset.UtcNow.AddHours(8)
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identity),
                authProperties);

            await _logService.LogLoginAsync(result.User.Id, GetClientIpAddress());

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return result.User.Role.Equals("Admin", StringComparison.OrdinalIgnoreCase)
                ? RedirectToAction("Dashboard", "Admin")
                : RedirectToAction("Index", "Sefer");
        }

        #endregion

        #region Cikis (Logout)

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Cikis()
        {
            int userId = GetCurrentUserId();

            if (userId > 0)
            {
                await _authService.RevokeRememberTokenAsync(userId);
                await _logService.LogLogoutAsync(userId, GetClientIpAddress());
            }

            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Giris");
        }

        #endregion

        #region SifremiUnuttum (Forgot Password)

        [HttpGet]
        public IActionResult SifremiUnuttum()
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Index", "Sefer");

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SifremiUnuttum(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                ModelState.AddModelError(string.Empty, "E-posta adresi gereklidir.");
                return View();
            }

            await _authService.RequestPasswordResetAsync(email);

            TempData["Bilgi"] = "Kayitli e-posta adresinize sifirlama baglantisi gonderildi.";
            return View();
        }

        #endregion

        #region SifreSifirla (Reset Password)

        [HttpGet]
        public async Task<IActionResult> SifreSifirla(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                TempData["Hata"] = "Gecersiz sifirlama baglantisi.";
                return RedirectToAction("Giris");
            }

            var isValid = await _authService.ValidatePasswordResetTokenAsync(token);
            if (!isValid)
            {
                TempData["Hata"] = "Gecersiz veya suresi dolmus baglanti.";
                return RedirectToAction("Giris");
            }

            ViewBag.Token = token;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SifreSifirla(string token, string yeniSifre, string yeniSifreTekrar)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                TempData["Hata"] = "Gecersiz sifirlama baglantisi.";
                return RedirectToAction("Giris");
            }

            if (yeniSifre != yeniSifreTekrar)
            {
                ModelState.AddModelError(string.Empty, "Sifreler eslesmiyor.");
                ViewBag.Token = token;
                return View();
            }

            if (string.IsNullOrWhiteSpace(yeniSifre) || yeniSifre.Length < 6)
            {
                ModelState.AddModelError(string.Empty, "Sifre en az 6 karakter olmalidir.");
                ViewBag.Token = token;
                return View();
            }

            var success = await _authService.ResetPasswordAsync(token, yeniSifre);

            if (!success)
            {
                TempData["Hata"] = "Gecersiz veya suresi dolmus baglanti.";
                return RedirectToAction("Giris");
            }

            TempData["Basari"] = "Sifreniz guncellendi. Giris yapabilirsiniz.";
            return RedirectToAction("Giris");
        }

        #endregion

        #region Profil

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Profil()
        {
            int userId = GetCurrentUserId();
            var user = await _authService.GetCurrentUserAsync(userId);

            if (user == null)
                return RedirectToAction("Giris");

            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> SifreDegistir(string mevcutSifre, string yeniSifre, string yeniSifreTekrar)
        {
            if (yeniSifre != yeniSifreTekrar)
            {
                TempData["Hata"] = "Yeni sifreler eslesmiyor.";
                return RedirectToAction("Profil");
            }

            if (string.IsNullOrWhiteSpace(yeniSifre) || yeniSifre.Length < 6)
            {
                TempData["Hata"] = "Yeni sifre en az 6 karakter olmalidir.";
                return RedirectToAction("Profil");
            }

            int userId = GetCurrentUserId();
            var success = await _authService.ChangePasswordAsync(userId, mevcutSifre, yeniSifre);

            if (!success)
            {
                TempData["Hata"] = "Mevcut sifre hatali.";
                return RedirectToAction("Profil");
            }

            await _logService.LogPasswordChangeAsync(userId, GetClientIpAddress());
            TempData["Basari"] = "Sifreniz basariyla degistirildi.";
            return RedirectToAction("Profil");
        }

        #endregion

        #region Helper Methods

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(userIdClaim, out int userId) ? userId : 0;
        }

        private string NormalizeEmail(string? email)
        {
            return (email ?? string.Empty).Trim().ToLowerInvariant();
        }

        private string GetAttemptCountKey(string email)
        {
            return $"auth:login:attempts:{GetClientIpAddress()}:{email}";
        }

        private string GetLockoutKey(string email)
        {
            return $"auth:login:lockout:{GetClientIpAddress()}:{email}";
        }

        private bool TryGetLockoutRemaining(string email, out TimeSpan remaining)
        {
            remaining = TimeSpan.Zero;

            if (!_memoryCache.TryGetValue<DateTimeOffset>(GetLockoutKey(email), out var lockoutUntil))
                return false;

            var now = DateTimeOffset.UtcNow;
            if (lockoutUntil <= now)
            {
                _memoryCache.Remove(GetLockoutKey(email));
                _memoryCache.Remove(GetAttemptCountKey(email));
                return false;
            }

            remaining = lockoutUntil - now;
            return true;
        }

        private void RegisterFailedAttempt(string email)
        {
            var attemptKey = GetAttemptCountKey(email);
            var lockoutKey = GetLockoutKey(email);

            var attempts = _memoryCache.Get<int?>(attemptKey) ?? 0;
            attempts++;

            _memoryCache.Set(attemptKey, attempts, FailedAttemptWindow);

            if (attempts >= MaxFailedLoginAttempts)
            {
                _memoryCache.Set(lockoutKey, DateTimeOffset.UtcNow.Add(LockoutDuration), LockoutDuration);
            }
        }

        private void ResetLoginProtection(string email)
        {
            _memoryCache.Remove(GetAttemptCountKey(email));
            _memoryCache.Remove(GetLockoutKey(email));
        }

        private string GetClientIpAddress()
        {
            return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        }

        #endregion
    }
}
