// ============================================================
// AccountController.cs — Authentication & Profile Controller
// ============================================================
// Handles all user authentication flows and profile management:
//
//   Login    — Authenticates an existing user with email + password.
//              Redirects to Admin or Member dashboard based on role.
//
//   SignUp   — Registers a new member account. Always assigns the "User"
//              role (admins are created by existing admins via AdminController).
//
//   Profile  — Allows a logged-in user to view and update their profile
//              information, including uploading a profile picture.
//
//   Logout   — Signs the user out and clears the authentication cookie.
//
// The controller itself has no [Authorize] attribute, so anonymous access
// is allowed by default for Login and SignUp. The [Authorize] attribute
// is applied per-action where login is required (Profile, Logout).
// ============================================================

using FitTrack.Models;
using FitTrack.Services;
using FitTrack.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace FitTrack.Controllers;

/// <summary>
/// Manages user registration, login, logout, and profile editing.
/// Uses primary constructor injection (C# 12 syntax) to receive Identity services.
/// </summary>
public class AccountController(
    UserManager<ApplicationUser> userManager,
    RoleManager<IdentityRole> roleManager,
    SignInManager<ApplicationUser> signInManager,
    IWebHostEnvironment environment) : Controller
{
    // ── Login ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// GET /Account/Login
    /// Displays the login form with an empty <see cref="LoginViewModel"/>.
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login() => View(new LoginViewModel());

    /// <summary>
    /// POST /Account/Login
    /// Validates the submitted credentials and signs the user in.
    ///
    /// Flow:
    ///   1. Validate the model (email format, password required).
    ///   2. Look up the user by email — return error if not found.
    ///   3. Call PasswordSignInAsync to verify the password and issue a cookie.
    ///   4. Redirect to Admin or Member dashboard depending on the user's role.
    ///
    /// lockoutOnFailure = false means failed logins do NOT lock the account.
    /// </summary>
    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken] // Protects against Cross-Site Request Forgery attacks.
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        // Find the user by email first (Identity stores email separately from UserName).
        var user = await userManager.FindByEmailAsync(model.Email);
        if (user is null)
        {
            // Don't reveal whether the email or password was wrong — use a generic message.
            ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            return View(model);
        }

        // Attempt sign-in using the UserName (which equals the email in this app).
        // model.RememberMe controls whether the cookie persists after the browser closes.
        var result = await signInManager.PasswordSignInAsync(user.UserName!, model.Password, model.RememberMe, lockoutOnFailure: false);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            return View(model);
        }

        // Sign-in succeeded — redirect based on role.
        return await userManager.IsInRoleAsync(user, "Admin")
            ? RedirectToAction("Dashboard", "Admin")
            : RedirectToAction("Dashboard", "Member");
    }

    // ── Sign Up ───────────────────────────────────────────────────────────────

    /// <summary>
    /// GET /Account/SignUp
    /// Displays the registration form with an empty <see cref="RegisterViewModel"/>.
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    public IActionResult SignUp() => View(new RegisterViewModel());

    /// <summary>
    /// POST /Account/SignUp
    /// Creates a new member account and redirects to the login page.
    ///
    /// Flow:
    ///   1. Validate the model (email, password, full name, etc.).
    ///   2. Ensure the "User" role exists (create it if missing).
    ///   3. Build an ApplicationUser and call CreateAsync with the hashed password.
    ///   4. Assign the "User" role and redirect to Login with a success message.
    ///
    /// A QR code token is generated immediately so the user can check in right away.
    /// EmailConfirmed = true skips the email verification step (not configured here).
    /// </summary>
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
            // Pre-generate a QR token so the member can use the check-in feature immediately.
            QrCodeToken = MemberQrCodeService.GenerateToken(),
            // Skip email confirmation (email service not configured).
            EmailConfirmed = true
        };

        // Attempt to create the user in the Identity database
        var result = await userManager.CreateAsync(user, model.Password);
        if (!result.Succeeded)
        {
            // Display all Identity validation errors (e.g., "Password too short").
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

    // ── Profile ───────────────────────────────────────────────────────────────

    /// <summary>
    /// GET /Account/Profile
    /// Loads the current user's profile information into the edit form.
    /// Requires the user to be logged in ([Authorize]).
    /// </summary>
    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Profile()
    {
        // GetUserAsync resolves the user from the current HTTP cookie/claims.
        var user = await userManager.GetUserAsync(User);
        if (user is null)
        {
            // Session expired or cookie invalid — send to login.
            return RedirectToAction(nameof(Login));
        }

        // Populate the ViewModel from the database record.
        return View(new EditProfileViewModel
        {
            FullName = user.FullName,
            Email = user.Email ?? string.Empty,
            PhoneNumber = user.PhoneNumber,
            Address = user.Address,
            CurrentProfilePicture = user.ProfilePicture // Shown in the form as a preview.
        });
    }

    /// <summary>
    /// POST /Account/Profile
    /// Saves updates to the current user's profile, including an optional profile picture upload.
    ///
    /// Profile picture upload rules (enforced here and not in the ViewModel):
    ///   - Allowed extensions: .jpg, .jpeg, .png, .webp
    ///   - Maximum size: 2 MB
    ///   - Saved to wwwroot/uploads/profiles/ with a GUID filename to prevent collisions.
    ///   - The DB stores the relative web path (/uploads/profiles/abc.jpg).
    /// </summary>
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
            // Re-populate the picture preview so the form doesn't lose the current image.
            model.CurrentProfilePicture = user.ProfilePicture;
            return View(model);
        }

        // Update basic profile fields.
        user.FullName = model.FullName;
        user.Email = model.Email;
        user.UserName = model.Email; // UserName must stay in sync with Email.
        user.PhoneNumber = model.PhoneNumber;
        user.Address = model.Address;

        // ── Profile Picture Upload ────────────────────────────────────────────
        if (model.ProfileImage is not null)
        {
            // Validate file extension — only image formats are accepted.
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            var extension = Path.GetExtension(model.ProfileImage.FileName).ToLowerInvariant();

            if (!allowedExtensions.Contains(extension))
            {
                ModelState.AddModelError(nameof(model.ProfileImage), "Only JPG, JPEG, PNG, and WEBP files are allowed.");
                model.CurrentProfilePicture = user.ProfilePicture;
                return View(model);
            }

            // Enforce a 2 MB file size limit.
            if (model.ProfileImage.Length > 2 * 1024 * 1024)
            {
                ModelState.AddModelError(nameof(model.ProfileImage), "Profile image must be 2 MB or smaller.");
                model.CurrentProfilePicture = user.ProfilePicture;
                return View(model);
            }

            // Build the upload path and create the directory if it doesn't exist.
            var uploadPath = Path.Combine(environment.WebRootPath, "uploads", "profiles");
            Directory.CreateDirectory(uploadPath);

            // Use a GUID filename to prevent filename collisions and hide the original name.
            var fileName = $"{Guid.NewGuid()}{extension}";
            var filePath = Path.Combine(uploadPath, fileName);

            // Stream the uploaded file to disk asynchronously.
            await using var stream = new FileStream(filePath, FileMode.Create);
            await model.ProfileImage.CopyToAsync(stream);

            // Store the web-accessible relative path in the user record.
            user.ProfilePicture = $"/uploads/profiles/{fileName}";
        }

        // Persist the updated user fields to the database via Identity.
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

    // ── Logout ────────────────────────────────────────────────────────────────

    /// <summary>
    /// POST /Account/Logout
    /// Signs the current user out by clearing the authentication cookie,
    /// then redirects to the public home page.
    ///
    /// Using [HttpPost] instead of [HttpGet] prevents CSRF logout attacks —
    /// a third-party page cannot trick the browser into logging the user out
    /// by embedding an img tag pointing to a logout URL.
    /// </summary>
    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        // Clear the Identity cookie — the user is now unauthenticated.
        await signInManager.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }
}
