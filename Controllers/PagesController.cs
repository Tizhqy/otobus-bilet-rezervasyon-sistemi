using Microsoft.AspNetCore.Mvc;

namespace OtobusBiletRezervasyon.Controllers
{
    public class PagesController : BaseController
    {
        public IActionResult About()
        {
            return View();
        }

        public IActionResult Careers()
        {
            return View();
        }

        public IActionResult Press()
        {
            ViewData["Title"] = "Press Room";
            return View("Template");
        }

        public IActionResult Blog()
        {
            ViewData["Title"] = "Blog";
            return View("Template");
        }

        public IActionResult HelpCenter()
        {
            ViewData["Title"] = "Help Center";
            return View("Template");
        }

        public IActionResult Contact()
        {
            ViewData["Title"] = "Contact Us";
            return View("Template");
        }

        public IActionResult RefundPolicy()
        {
            ViewData["Title"] = "Refund Policy";
            return View("Template");
        }

        public IActionResult Faq()
        {
            ViewData["Title"] = "Frequently Asked Questions";
            return View("Template");
        }

        public IActionResult Privacy()
        {
            ViewData["Title"] = "Privacy Policy";
            return View("Template");
        }

        public IActionResult Terms()
        {
            ViewData["Title"] = "Terms of Use";
            return View("Template");
        }

        public IActionResult CookiePolicy()
        {
            ViewData["Title"] = "Cookie Policy";
            return View("Template");
        }
    }
}