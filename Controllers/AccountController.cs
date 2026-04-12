using FitTrack.Models;
using Microsoft.AspNetCore.Mvc;

namespace FitTrack.Controllers
{
    public class AccountController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
        public IActionResult Login()
        {
            return View();
        }
        [HttpGet]
         public IActionResult SignUp()
        {
            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SignUp(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                // TODO: Implement registration logic (save to DB)
                return RedirectToAction("Index", "Home");
            }
            return View(model);
        }
    }
}
