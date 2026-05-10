// ============================================================
// SeedData.cs — Database Seeding & Startup Data
// ============================================================
// This static class is called once at application startup (see Program.cs).
// Its purpose is to:
//   1. Apply any pending EF Core database migrations automatically.
//   2. Ensure the "Admin" and "User" roles exist in the database.
//   3. Create the default system administrator account if it does not exist.
//   4. Backfill any existing users who are missing a QR code token.
//
// This means developers never need to run manual SQL inserts or seed
// scripts — the app self-initialises on first boot.
// ============================================================

using FitTrack.Models;
using FitTrack.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FitTrack.Data;

/// <summary>
/// Provides startup data seeding for the FitTrack application.
/// Called from Program.cs after the application is built.
/// </summary>
public static class SeedData
{
    /// <summary>
    /// Runs database migrations, seeds roles and the default admin user,
    /// and ensures all existing users have a QR code token.
    /// </summary>
    /// <param name="serviceProvider">
    /// The root DI service provider from <c>app.Services</c>.
    /// A new scope is created internally so EF Core services are properly scoped.
    /// </param>
    public static async Task InitializeAsync(IServiceProvider serviceProvider)
    {
        // Create a DI scope so we can resolve Scoped services (DbContext, UserManager, etc.).
        // Without a scope, resolving Scoped services from the root provider would throw.
        using var scope = serviceProvider.CreateScope();
        var services = scope.ServiceProvider;

        // Resolve the services we need for seeding.
        var context = services.GetRequiredService<ApplicationDbContext>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

        // ── Step 1: Apply Pending Migrations ─────────────────────────────────
        // MigrateAsync() creates the database if it doesn't exist, then applies
        // any EF Core migrations that haven't been run yet (like "Initial Create").
        // This replaces the need to run `dotnet ef database update` manually.
        await context.Database.MigrateAsync();

        // ── Step 2: Seed Roles ────────────────────────────────────────────────
        // Ensure both application roles exist. Identity uses these for [Authorize(Roles = "...")]
        // checks throughout the controllers.
        //   "Admin" — full access to the admin panel.
        //   "User"  — regular member access to the member dashboard.
        foreach (var role in new[] { "Admin", "User" })
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        // ── Step 3: Seed Default Admin Account ───────────────────────────────
        // These credentials are only used on the very first boot.
        // IMPORTANT: Change the password after the first login in production.
        const string adminEmail = "admin@fittrack.local";
        const string adminPassword = "Admin123!";

        var adminUser = await userManager.FindByEmailAsync(adminEmail);

        if (adminUser is null)
        {
            // The admin account does not exist yet — create it now.
            adminUser = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                FullName = "System Administrator",
                Role = "Admin",
                // Generate a unique, cryptographically-random QR token so the
                // admin user also has a valid token from the start.
                QrCodeToken = MemberQrCodeService.GenerateToken(),
                // Skip email confirmation for the seeded admin account.
                EmailConfirmed = true
            };

            var createAdmin = await userManager.CreateAsync(adminUser, adminPassword);
            if (createAdmin.Succeeded)
            {
                // Assign the "Admin" role so [Authorize(Roles = "Admin")] works immediately.
                await userManager.AddToRoleAsync(adminUser, "Admin");
            }
        }
        else if (!await userManager.IsInRoleAsync(adminUser, "Admin"))
        {
            // Edge case: the admin user exists in the database but somehow lost their role
            // (e.g., manual DB manipulation). Restore the role and the Role field.
            adminUser.Role = "Admin";
            await userManager.UpdateAsync(adminUser);
            await userManager.AddToRoleAsync(adminUser, "Admin");
        }

        // ── Step 4: Backfill Missing QR Tokens ───────────────────────────────
        // If users were created before the QrCodeToken column was added,
        // they will have a null/empty token. Generate tokens for them now
        // so they can use the QR check-in feature without needing to log out/in.
        var usersMissingQrTokens = await context.Users
            .Where(u => string.IsNullOrWhiteSpace(u.QrCodeToken))
            .ToListAsync();

        foreach (var user in usersMissingQrTokens)
        {
            user.QrCodeToken = MemberQrCodeService.GenerateToken();
        }

        // Only hit the database if there is something to save (avoids an unnecessary round-trip).
        if (usersMissingQrTokens.Count > 0)
        {
            await context.SaveChangesAsync();
        }
    }
}
