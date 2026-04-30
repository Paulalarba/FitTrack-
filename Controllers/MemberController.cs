using FitTrack.Data;
using FitTrack.Models;
using FitTrack.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FitTrack.Controllers;

[Authorize]
public class MemberController(ApplicationDbContext context, UserManager<ApplicationUser> userManager) : Controller
{
    private const int PageSize = 8;

    public async Task<IActionResult> Dashboard()
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null)
        {
            return RedirectToAction("Login", "Account");
        }

        if (await userManager.IsInRoleAsync(user, "Admin"))
        {
            return RedirectToAction("Dashboard", "Admin");
        }

        var model = new MemberDashboardViewModel
        {
            User = user,
            CurrentMembership = await context.Memberships
                .Where(m => m.UserId == user.Id)
                .OrderByDescending(m => m.EndDate)
                .FirstOrDefaultAsync(),
            RecentTransactions = await context.Transactions
                .Where(t => t.UserId == user.Id)
                .OrderByDescending(t => t.PaymentDate)
                .Take(5)
                .ToListAsync()
        };

        return View(model);
    }

    public async Task<IActionResult> Transactions(int page = 1)
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null)
        {
            return RedirectToAction("Login", "Account");
        }

        var query = context.Transactions.Where(t => t.UserId == user.Id);
        var totalCount = await query.CountAsync();
        var transactions = await query
            .OrderByDescending(t => t.PaymentDate)
            .Skip((page - 1) * PageSize)
            .Take(PageSize)
            .ToListAsync();

        return View(new MemberTransactionsViewModel
        {
            Transactions = transactions,
            Page = page,
            TotalPages = (int)Math.Ceiling(totalCount / (double)PageSize)
        });
    }

    [HttpGet]
    public IActionResult RequestPayment() => View(new TransactionRequestViewModel { Amount = 850M, MembershipPlan = "Classic Membership" });

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RequestPayment(TransactionRequestViewModel model)
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null)
        {
            return RedirectToAction("Login", "Account");
        }

        if (model.MembershipPlan == "Classic Membership" && model.Amount != 850M)
        {
            ModelState.AddModelError("Amount", "Invalid amount for Classic Membership. The price is ₱850.");
        }
        else if (model.MembershipPlan == "PF Black Card Membership" && model.Amount != 1500M)
        {
            ModelState.AddModelError("Amount", "Invalid amount for PF Black Card Membership. The price is ₱1,500.");
        }
        else if (model.MembershipPlan != "Classic Membership" && model.MembershipPlan != "PF Black Card Membership")
        {
            ModelState.AddModelError("MembershipPlan", "Invalid membership plan selected.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var transaction = new Transaction
        {
            UserId = user.Id,
            Amount = model.Amount,
            MembershipPlan = model.MembershipPlan,
            Notes = model.Notes,
            Status = "Pending",
            PaymentDate = DateTime.UtcNow
        };

        context.Transactions.Add(transaction);

        var membership = await context.Memberships
            .Where(m => m.UserId == user.Id)
            .OrderByDescending(m => m.EndDate)
            .FirstOrDefaultAsync();

        if (membership is null)
        {
            context.Memberships.Add(new Membership
            {
                UserId = user.Id,
                PlanName = model.MembershipPlan,
                StartDate = DateTime.UtcNow.Date,
                EndDate = DateTime.UtcNow.Date,
                Status = "Pending",
                MonthlyFee = model.Amount
            });
        }
        else if (membership.Status != "Active")
        {
            membership.PlanName = model.MembershipPlan;
            membership.MonthlyFee = model.Amount;
            membership.Status = "Pending";
        }

        await context.SaveChangesAsync();
        TempData["StatusMessage"] = "Payment request submitted for admin approval.";
        return RedirectToAction(nameof(Transactions));
    }
}
