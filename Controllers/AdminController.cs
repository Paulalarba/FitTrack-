using FitTrack.Data;
using FitTrack.Models;
using FitTrack.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FitTrack.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController(
    ApplicationDbContext context,
    UserManager<ApplicationUser> userManager,
    RoleManager<IdentityRole> roleManager) : Controller
{
    private const int PageSize = 10;

    public async Task<IActionResult> Dashboard()
    {
        var model = new AdminDashboardViewModel
        {
            TotalUsers = await context.Users.CountAsync(),
            TotalTransactions = await context.Transactions.CountAsync(),
            ActiveMemberships = await context.Memberships.CountAsync(m => m.Status == "Active" && m.EndDate >= DateTime.UtcNow.Date),
            RecentUsers = await context.Users.OrderByDescending(u => u.JoinedDate).Take(5).ToListAsync(),
            RecentTransactions = await context.Transactions.Include(t => t.User).OrderByDescending(t => t.PaymentDate).Take(5).ToListAsync()
        };

        return View(model);
    }

    public async Task<IActionResult> Users(string? searchTerm, int page = 1)
    {
        var query = context.Users.AsQueryable();
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(u =>
                u.FullName.Contains(searchTerm) ||
                (u.Email != null && u.Email.Contains(searchTerm)) ||
                u.Id.Contains(searchTerm));
        }

        var totalCount = await query.CountAsync();
        var users = await query
            .OrderByDescending(u => u.JoinedDate)
            .Skip((page - 1) * PageSize)
            .Take(PageSize)
            .Select(u => new AdminUserListItemViewModel
            {
                Id = u.Id,
                FullName = u.FullName,
                Email = u.Email ?? string.Empty,
                Role = u.Role,
                JoinedDate = u.JoinedDate,
                MembershipStatus = context.Memberships
                    .Where(m => m.UserId == u.Id)
                    .OrderByDescending(m => m.EndDate)
                    .Select(m => m.Status)
                    .FirstOrDefault() ?? "No membership"
            })
            .ToListAsync();

        return View(new AdminUsersViewModel
        {
            Users = users,
            SearchTerm = searchTerm,
            Page = page,
            TotalPages = (int)Math.Ceiling(totalCount / (double)PageSize)
        });
    }

    [HttpGet]
    public IActionResult CreateUser() => View(new ManageUserViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateUser(ManageUserViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        if (string.IsNullOrWhiteSpace(model.Password))
        {
            ModelState.AddModelError(nameof(model.Password), "Password is required when creating a user.");
            return View(model);
        }

        if (!await roleManager.RoleExistsAsync(model.Role))
        {
            await roleManager.CreateAsync(new IdentityRole(model.Role));
        }

        var user = new ApplicationUser
        {
            FullName = model.FullName,
            Email = model.Email,
            UserName = model.Email,
            PhoneNumber = model.PhoneNumber,
            Address = model.Address,
            Role = model.Role,
            EmailConfirmed = true
        };

        var result = await userManager.CreateAsync(user, model.Password);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        await userManager.AddToRoleAsync(user, model.Role);
        TempData["StatusMessage"] = "User account created.";
        return RedirectToAction(nameof(Users));
    }

    [HttpGet]
    public async Task<IActionResult> EditUser(string id)
    {
        var user = await userManager.FindByIdAsync(id);
        if (user is null)
        {
            return NotFound();
        }

        return View(new ManageUserViewModel
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email ?? string.Empty,
            PhoneNumber = user.PhoneNumber,
            Address = user.Address,
            Role = user.Role
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditUser(ManageUserViewModel model)
    {
        if (!ModelState.IsValid || string.IsNullOrWhiteSpace(model.Id))
        {
            return View(model);
        }

        var user = await userManager.FindByIdAsync(model.Id);
        if (user is null)
        {
            return NotFound();
        }

        user.FullName = model.FullName;
        user.Email = model.Email;
        user.UserName = model.Email;
        user.PhoneNumber = model.PhoneNumber;
        user.Address = model.Address;
        user.Role = model.Role;

        var updateResult = await userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            foreach (var error in updateResult.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        var existingRoles = await userManager.GetRolesAsync(user);
        foreach (var existingRole in existingRoles)
        {
            await userManager.RemoveFromRoleAsync(user, existingRole);
        }

        if (!await roleManager.RoleExistsAsync(model.Role))
        {
            await roleManager.CreateAsync(new IdentityRole(model.Role));
        }

        await userManager.AddToRoleAsync(user, model.Role);

        if (!string.IsNullOrWhiteSpace(model.Password))
        {
            var resetToken = await userManager.GeneratePasswordResetTokenAsync(user);
            var passwordResult = await userManager.ResetPasswordAsync(user, resetToken, model.Password);
            if (!passwordResult.Succeeded)
            {
                foreach (var error in passwordResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }

                return View(model);
            }
        }

        TempData["StatusMessage"] = "User account updated.";
        return RedirectToAction(nameof(Users));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteUser(string id)
    {
        var user = await userManager.FindByIdAsync(id);
        if (user is null)
        {
            return NotFound();
        }

        if (user.Email?.Equals("admin@fittrack.local", StringComparison.OrdinalIgnoreCase) == true)
        {
            TempData["StatusMessage"] = "The seeded administrator account cannot be deleted.";
            return RedirectToAction(nameof(Users));
        }

        var memberships = await context.Memberships.Where(m => m.UserId == id).ToListAsync();
        var transactions = await context.Transactions.Where(t => t.UserId == id).ToListAsync();
        context.Memberships.RemoveRange(memberships);
        context.Transactions.RemoveRange(transactions);
        await context.SaveChangesAsync();

        await userManager.DeleteAsync(user);
        TempData["StatusMessage"] = "User account deleted.";
        return RedirectToAction(nameof(Users));
    }

    public async Task<IActionResult> Transactions(string? searchTerm, string? statusFilter, int page = 1)
    {
        var query = context.Transactions.Include(t => t.User).AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(t =>
                t.MembershipPlan.Contains(searchTerm) ||
                t.Id.ToString().Contains(searchTerm) ||
                (t.User != null && (t.User.FullName.Contains(searchTerm) || (t.User.Email != null && t.User.Email.Contains(searchTerm)))));
        }

        if (!string.IsNullOrWhiteSpace(statusFilter))
        {
            query = query.Where(t => t.Status == statusFilter);
        }

        var totalCount = await query.CountAsync();
        var transactions = await query
            .OrderByDescending(t => t.PaymentDate)
            .Skip((page - 1) * PageSize)
            .Take(PageSize)
            .ToListAsync();

        return View(new AdminTransactionsViewModel
        {
            Transactions = transactions,
            SearchTerm = searchTerm,
            StatusFilter = statusFilter,
            Page = page,
            TotalPages = (int)Math.Ceiling(totalCount / (double)PageSize)
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateTransactionStatus(int id, string status)
    {
        var transaction = await context.Transactions.FirstOrDefaultAsync(t => t.Id == id);
        if (transaction is null)
        {
            return NotFound();
        }

        transaction.Status = status;

        if (status == "Approved")
        {
            var startDate = DateTime.UtcNow.Date;
            var endDate = transaction.MembershipPlan.Equals("Annual", StringComparison.OrdinalIgnoreCase)
                ? startDate.AddYears(1)
                : startDate.AddMonths(1);

            var membership = await context.Memberships
                .Where(m => m.UserId == transaction.UserId)
                .OrderByDescending(m => m.EndDate)
                .FirstOrDefaultAsync();

            if (membership is null)
            {
                membership = new Membership
                {
                    UserId = transaction.UserId,
                    PlanName = transaction.MembershipPlan,
                    StartDate = startDate,
                    EndDate = endDate,
                    Status = "Active",
                    MonthlyFee = transaction.Amount
                };
                context.Memberships.Add(membership);
            }
            else
            {
                membership.PlanName = transaction.MembershipPlan;
                membership.StartDate = startDate;
                membership.EndDate = endDate;
                membership.Status = "Active";
                membership.MonthlyFee = transaction.Amount;
            }
        }
        else if (status == "Rejected")
        {
            var membership = await context.Memberships
                .Where(m => m.UserId == transaction.UserId)
                .OrderByDescending(m => m.EndDate)
                .FirstOrDefaultAsync();

            if (membership is not null && membership.Status == "Pending")
            {
                membership.Status = "Rejected";
            }
        }

        await context.SaveChangesAsync();
        TempData["StatusMessage"] = $"Transaction #{transaction.Id} marked as {status}.";
        return RedirectToAction(nameof(Transactions));
    }
}
