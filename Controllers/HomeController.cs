using FitTrack.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace FitTrack.Controllers;

public class HomeController(UserManager<ApplicationUser> userManager) : Controller
{
    [AllowAnonymous]
    public async Task<IActionResult> Index()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            var user = await userManager.GetUserAsync(User);
            if (user is not null && await userManager.IsInRoleAsync(user, "Admin"))
            {
                return RedirectToAction("Dashboard", "Admin");
            }

            return RedirectToAction("Dashboard", "Member");
        }

        return View();
    }

    public IActionResult AboutUs()
    {
        return View();
    }

    [AllowAnonymous]
    public IActionResult AccessDenied() => View();
}
