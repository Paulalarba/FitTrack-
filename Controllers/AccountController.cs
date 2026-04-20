using FitTrack.Models;
using FitTrack.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace FitTrack.Controllers;

public class AccountController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager) : Controller
{
    public IActionResult Index() => View();

    public IActionResult Login() => View();

    [HttpPost]
    public async Task<IActionResult> Login(string email, string password)
    {
        var result = await signInManager.PasswordSignInAsync(email, password, false, false);
        if (result.Succeeded) return RedirectToAction("Index", "Home");

        ModelState.AddModelError("", "Invalid login attempt");
        return View();
    }

    [HttpGet]
    public IActionResult SignUp() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SignUp(RegisterViewModel model)
    {
        if (ModelState.IsValid)
        {
            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                FullName = model.FullName,
                Role = "User"
            };

            var result = await userManager.CreateAsync(user, model.Password);
            if (result.Succeeded)
            {
                await signInManager.SignInAsync(user, isPersistent: false);
                return RedirectToAction("Index", "Home");
            }
            foreach (var error in result.Errors) ModelState.AddModelError("", error.Description);
        }
        return View(model);
    }

    public async Task<IActionResult> Logout()
    {
        await signInManager.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }
}
