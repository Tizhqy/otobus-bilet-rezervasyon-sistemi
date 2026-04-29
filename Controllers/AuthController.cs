using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;
using OtobusBiletRezervasyon.DTOs.Auth;
using OtobusBiletRezervasyon.Services.Interfaces;

namespace OtobusBiletRezervasyon.Controllers
{
    public class AuthController : BaseController
    {
        private readonly IAuthService _authService;
        private readonly ILogService _logService;

        public AuthController(IAuthService authService, ILogService logService)
        {
            _authService = authService;
            _logService = logService;
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
        [EnableRateLimiting("AuthLoginPolicy")]
        public async Task<IActionResult> Giris(LoginDto model, string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Index", "Sefer");

            if (!ModelState.IsValid)
            {
                ViewBag.ReturnUrl = returnUrl;
                return View(model);
            }

            var result = await _authService.LoginAsync(model);

            if (!result.Success)
            {
                ModelState.AddModelError(string.Empty, result.Message);
                ViewBag.ReturnUrl = returnUrl;
                return View(model);
            }

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
        [EnableRateLimiting("PasswordResetPolicy")]
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

            var isValidToken = await _authService.IsPasswordResetTokenValidAsync(token);
            if (!isValidToken)
            {
                TempData["Hata"] = "Gecersiz veya suresi dolmus baglanti.";
                return RedirectToAction("Giris");
            }

            ViewBag.Token = token;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("PasswordResetConfirmPolicy")]
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

            if (!IsStrongPassword(yeniSifre))
            {
                ModelState.AddModelError(string.Empty,
                    $"Sifre en az {AppConfig.MinPasswordLength} karakter olmali; en az bir buyuk harf, bir kucuk harf ve bir rakam icermelidir.");
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
        [EnableRateLimiting("PasswordChangePolicy")]
        public async Task<IActionResult> SifreDegistir(string mevcutSifre, string yeniSifre, string yeniSifreTekrar)
        {
            if (yeniSifre != yeniSifreTekrar)
            {
                TempData["Hata"] = "Yeni sifreler eslesmiyor.";
                return RedirectToAction("Profil");
            }

            if (!IsStrongPassword(yeniSifre))
            {
                TempData["Hata"] = $"Yeni sifre en az {AppConfig.MinPasswordLength} karakter olmali; en az bir buyuk harf, bir kucuk harf ve bir rakam icermelidir.";
                return RedirectToAction("Profil");
            }

            int userId = GetCurrentUserId();
            var success = await _authService.ChangePasswordAsync(userId, mevcutSifre, yeniSifre);

            if (!success)
            {
                await _logService.LogAsync(userId, "PASSWORD_CHANGE_FAILED", "Mevcut sifre dogrulamasi basarisiz.", GetClientIpAddress());
                TempData["Hata"] = "Mevcut sifre hatali.";
                return RedirectToAction("Profil");
            }

            await _logService.LogPasswordChangeAsync(userId, GetClientIpAddress());
            TempData["Basari"] = "Sifreniz basariyla degistirildi.";
            return RedirectToAction("Profil");
        }

        #endregion


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
