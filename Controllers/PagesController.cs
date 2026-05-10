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
            return View();
        }

        public IActionResult Blog()
        {
            ViewData["Title"] = "Blog";
            return View();
        }

        public IActionResult HelpCenter()
        {
            ViewData["Title"] = "Help Center";
            return View();
        }

        public IActionResult Contact()
        {
            ViewData["Title"] = "Contact Us";
            return View();
        }

        public IActionResult RefundPolicy()
        {
            ViewData["Title"] = "Refund Policy";
            return View();
        }

        public IActionResult Faq()
        {
            ViewData["Title"] = "Frequently Asked Questions";
            return View();
        }

        public IActionResult Privacy()
        {
            ViewData["Title"] = "Privacy Policy";
            return View();
        }

        public IActionResult Terms()
        {
            ViewData["Title"] = "Terms of Use";
            return View();
        }

        public IActionResult CookiePolicy()
        {
            ViewData["Title"] = "Cookie Policy";
            return View();
        }
    }
}