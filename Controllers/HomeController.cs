// ============================================================
// HomeController.cs — Public Landing & Navigation Controller
// ============================================================
// Handles the publicly-accessible pages of FitTrack:
//   - Index   : The landing/home page. If the user is already logged in,
//               redirects them to the appropriate dashboard automatically.
//   - AboutUs : Static "About" information page.
//   - Contact : Static contact page.
//   - AccessDenied : Shown by Identity when a logged-in user attempts to
//                    access a route they do not have permission for.
//
// Most actions here are [AllowAnonymous] because they are intended for
// visitors who have not yet logged in.
// ============================================================

using FitTrack.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace FitTrack.Controllers;

/// <summary>
/// Controller for public-facing pages and global navigation.
/// Uses constructor injection to receive the UserManager for role checks.
/// </summary>
public class HomeController(UserManager<ApplicationUser> userManager) : Controller
{
    /// <summary>
    /// GET /
    /// Renders the public home/landing page.
    ///
    /// If the visitor is already authenticated, they are redirected to their
    /// role-appropriate dashboard instead of seeing the landing page.
    /// This prevents logged-in users from having to manually navigate away.
    /// </summary>
    [AllowAnonymous] // Anyone (logged in or not) can access this route.
    public async Task<IActionResult> Index()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            // User is logged in — find out who they are.
            var user = await userManager.GetUserAsync(User);

            // Admins go to the Admin Dashboard; everyone else goes to Member Dashboard.
            if (user is not null && await userManager.IsInRoleAsync(user, "Admin"))
            {
                return RedirectToAction("Dashboard", "Admin");
            }

            return RedirectToAction("Dashboard", "Member");
        }

        // User is not logged in — show the public landing page (Views/Home/Index.cshtml).
        return View();
    }

    /// <summary>
    /// GET /Home/AboutUs
    /// Renders the static "About Us" page (Views/Home/AboutUs.cshtml).
    /// </summary>
    public IActionResult AboutUs()
    {
        return View();
    }

    /// <summary>
    /// GET /Home/AccessDenied
    /// Shown automatically by ASP.NET Core Identity when a logged-in user
    /// tries to access a route their role does not permit
    /// (e.g., a "User" role member trying to visit /Admin/Dashboard).
    /// Configured in Program.cs: options.AccessDeniedPath = "/Home/AccessDenied".
    /// </summary>
    [AllowAnonymous]
    public IActionResult AccessDenied() => View();

    /// <summary>
    /// GET /Home/Contact
    /// Renders the static Contact page (Views/Home/Contact.cshtml).
    /// </summary>
    public IActionResult Contact()
    {
        return View();
    }
}
