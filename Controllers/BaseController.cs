using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace OtobusBiletRezervasyon.Controllers
{
    /// <summary>
    /// Tum controller'larin turetildigi temel sinif.
    /// Ortak yardimci metodlari burada topluyoruz (DRY prensibi).
    /// </summary>
    public abstract class BaseController : Controller
    {
        /// <summary>
        /// Oturum acmis kullanicinin ID'sini dondurur.
        /// </summary>
        protected int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(userIdClaim, out int userId) ? userId : 0;
        }

        /// <summary>
        /// Istekta bulunan istemcinin IP adresini dondurur.
        /// </summary>
        protected string GetClientIpAddress()
        {
            return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        }

        /// <summary>
        /// Odeme yontemini normalize eder (CreditCard, DebitCard, Paypal).
        /// </summary>
        protected static bool TryNormalizePaymentMethod(string? method, out string normalizedMethod)
        {
            normalizedMethod = string.Empty;
            if (string.IsNullOrWhiteSpace(method))
                return false;

            var compact = method
                .Replace("_", string.Empty, StringComparison.Ordinal)
                .Replace(" ", string.Empty, StringComparison.Ordinal)
                .Trim()
                .ToLowerInvariant();

            normalizedMethod = compact switch
            {
                "creditcard" => "CreditCard",
                "debitcard" => "DebitCard",
                "paypal" => "Paypal",
                _ => string.Empty
            };

            return !string.IsNullOrEmpty(normalizedMethod);
        }
    }
}
