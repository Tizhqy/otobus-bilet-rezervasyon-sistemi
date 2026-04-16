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

    }
}
