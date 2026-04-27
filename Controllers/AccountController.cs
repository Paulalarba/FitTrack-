using FitTrack.Models;
using FitTrack.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace FitTrack.Controllers;

public class AccountController(
    UserManager<ApplicationUser> userManager,
    RoleManager<IdentityRole> roleManager,
    SignInManager<ApplicationUser> signInManager,
    IWebHostEnvironment environment) : Controller
{
    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login() => View(new LoginViewModel());

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await userManager.FindByEmailAsync(model.Email);
        if (user is null)
        {
            ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            return View(model);
        }

        var result = await signInManager.PasswordSignInAsync(user.UserName!, model.Password, model.RememberMe, lockoutOnFailure: false);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            return View(model);
        }

        return await userManager.IsInRoleAsync(user, "Admin")
            ? RedirectToAction("Dashboard", "Admin")
            : RedirectToAction("Dashboard", "Member");
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult SignUp() => View(new RegisterViewModel());

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SignUp(RegisterViewModel model)
    {
        // Validate the input model
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        // Ensure the "User" role exists in the database
        if (!await roleManager.RoleExistsAsync("User"))
        {
            await roleManager.CreateAsync(new IdentityRole("User"));
        }

        // Map the ViewModel data to the ApplicationUser entity
        var user = new ApplicationUser
        {
            UserName = model.Email,
            Email = model.Email,
            FullName = model.FullName,
            PhoneNumber = model.PhoneNumber,
            Role = "User",
            EmailConfirmed = true
        };

        // Attempt to create the user in the Identity database
        var result = await userManager.CreateAsync(user, model.Password);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        //Assign the user to the "User" role
        await userManager.AddToRoleAsync(user, "User");

        // Set a success message to be displayed on the Login page
        TempData["StatusMessage"] = "Account created successfully! Please log in.";

        //  Redirect to the Login action
        return RedirectToAction(nameof(Login));
    }


    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Profile()
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null)
        {
            return RedirectToAction(nameof(Login));
        }

        return View(new EditProfileViewModel
        {
            FullName = user.FullName,
            Email = user.Email ?? string.Empty,
            PhoneNumber = user.PhoneNumber,
            Address = user.Address,
            CurrentProfilePicture = user.ProfilePicture
        });
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Profile(EditProfileViewModel model)
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null)
        {
            return RedirectToAction(nameof(Login));
        }

        if (!ModelState.IsValid)
        {
            model.CurrentProfilePicture = user.ProfilePicture;
            return View(model);
        }

        user.FullName = model.FullName;
        user.Email = model.Email;
        user.UserName = model.Email;
        user.PhoneNumber = model.PhoneNumber;
        user.Address = model.Address;

        if (model.ProfileImage is not null)
        {
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            var extension = Path.GetExtension(model.ProfileImage.FileName).ToLowerInvariant();

            if (!allowedExtensions.Contains(extension))
            {
                ModelState.AddModelError(nameof(model.ProfileImage), "Only JPG, JPEG, PNG, and WEBP files are allowed.");
                model.CurrentProfilePicture = user.ProfilePicture;
                return View(model);
            }

            if (model.ProfileImage.Length > 2 * 1024 * 1024)
            {
                ModelState.AddModelError(nameof(model.ProfileImage), "Profile image must be 2 MB or smaller.");
                model.CurrentProfilePicture = user.ProfilePicture;
                return View(model);
            }

            var uploadPath = Path.Combine(environment.WebRootPath, "uploads", "profiles");
            Directory.CreateDirectory(uploadPath);

            var fileName = $"{Guid.NewGuid()}{extension}";
            var filePath = Path.Combine(uploadPath, fileName);

            await using var stream = new FileStream(filePath, FileMode.Create);
            await model.ProfileImage.CopyToAsync(stream);
            user.ProfilePicture = $"/uploads/profiles/{fileName}";
        }

        var result = await userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            model.CurrentProfilePicture = user.ProfilePicture;
            return View(model);
        }

        TempData["StatusMessage"] = "Profile updated successfully.";
        return RedirectToAction(nameof(Profile));
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await signInManager.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }
}
