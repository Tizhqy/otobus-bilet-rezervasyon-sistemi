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

            TempData["Basari"] = "Registration successful. You can now log in.";
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
                try
                {
                    await _authService.RevokeRememberTokenAsync(userId);
                    await _logService.LogLogoutAsync(userId, GetClientIpAddress());
                }
                catch (Exception)
                {
                    // If the user has been deleted from the database (e.g., database reset) or 
                    // if another logging error occurs, swallow the error so the user isn't blocked from logging out.
                }
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
                ModelState.AddModelError(string.Empty, "Email address is required.");
                return View();
            }

            await _authService.RequestPasswordResetAsync(email);

            TempData["Bilgi"] = "A password reset link has been sent to your registered email address.";
            return View();
        }

        #endregion

        #region SifreSifirla (Reset Password)

        [HttpGet]
        public async Task<IActionResult> SifreSifirla(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                TempData["Hata"] = "Invalid password reset link.";
                return RedirectToAction("Giris");
            }

            var isValidToken = await _authService.IsPasswordResetTokenValidAsync(token);
            if (!isValidToken)
            {
                TempData["Hata"] = "Invalid or expired reset link.";
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
                TempData["Hata"] = "Invalid password reset link.";
                return RedirectToAction("Giris");
            }

            if (yeniSifre != yeniSifreTekrar)
            {
                ModelState.AddModelError(string.Empty, "Passwords do not match.");
                ViewBag.Token = token;
                return View();
            }

            if (!IsStrongPassword(yeniSifre))
            {
                ModelState.AddModelError(string.Empty,
                    $"Password must be at least {AppConfig.MinPasswordLength} characters long; and must contain at least one uppercase letter, one lowercase letter, and one number.");
                ViewBag.Token = token;
                return View();
            }

            var success = await _authService.ResetPasswordAsync(token, yeniSifre);

            if (!success)
            {
                TempData["Hata"] = "Invalid or expired reset link.";
                return RedirectToAction("Giris");
            }

            TempData["Basari"] = "Your password has been updated. You can now log in.";
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
                TempData["Hata"] = "New passwords do not match.";
                return RedirectToAction("Profil");
            }

            if (!IsStrongPassword(yeniSifre))
            {
                TempData["Hata"] = $"New password must be at least {AppConfig.MinPasswordLength} characters long; and must contain at least one uppercase letter, one lowercase letter, and one number.";
                return RedirectToAction("Profil");
            }

            int userId = GetCurrentUserId();
            var success = await _authService.ChangePasswordAsync(userId, mevcutSifre, yeniSifre);

            if (!success)
            {
                await _logService.LogAsync(userId, "PASSWORD_CHANGE_FAILED", "Current password verification failed.", GetClientIpAddress());
                TempData["Hata"] = "Current password is incorrect.";
                return RedirectToAction("Profil");
            }

            await _logService.LogPasswordChangeAsync(userId, GetClientIpAddress());
            TempData["Basari"] = "Your password has been successfully changed.";
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
