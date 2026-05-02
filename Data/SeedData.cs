using FitTrack.Models;
using FitTrack.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FitTrack.Data;

public static class SeedData
{
    public static async Task InitializeAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var services = scope.ServiceProvider;

        var context = services.GetRequiredService<ApplicationDbContext>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

        await context.Database.MigrateAsync();

        foreach (var role in new[] { "Admin", "User" })
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        const string adminEmail = "admin@fittrack.local";
        const string adminPassword = "Admin123!";

        var adminUser = await userManager.FindByEmailAsync(adminEmail);
        if (adminUser is null)
        {
            adminUser = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                FullName = "System Administrator",
                Role = "Admin",
                QrCodeToken = MemberQrCodeService.GenerateToken(),
                EmailConfirmed = true
            };

            var createAdmin = await userManager.CreateAsync(adminUser, adminPassword);
            if (createAdmin.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, "Admin");
            }
        }
        else if (!await userManager.IsInRoleAsync(adminUser, "Admin"))
        {
            adminUser.Role = "Admin";
            await userManager.UpdateAsync(adminUser);
            await userManager.AddToRoleAsync(adminUser, "Admin");
        }

        var usersMissingQrTokens = await context.Users
            .Where(u => string.IsNullOrWhiteSpace(u.QrCodeToken))
            .ToListAsync();

        foreach (var user in usersMissingQrTokens)
        {
            user.QrCodeToken = MemberQrCodeService.GenerateToken();
        }

        if (usersMissingQrTokens.Count > 0)
        {
            await context.SaveChangesAsync();
        }
    }
}
