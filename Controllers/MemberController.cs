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
            WalletBalance = await context.Wallets
                .Where(w => w.UserId == user.Id)
                .Select(w => (decimal?)w.Balance)
                .FirstOrDefaultAsync() ?? 0M,
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

    public async Task<IActionResult> PaymentOptions()
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null)
        {
            return RedirectToAction("Login", "Account");
        }

        var accounts = await context.PaymentAccounts
            .OrderBy(p => p.PaymentType)
            .ThenBy(p => p.AccountName)
            .ToListAsync();

        return View(accounts);
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

        var wallet = await context.Wallets.FirstOrDefaultAsync(w => w.UserId == user.Id);
        if (wallet is null || wallet.Balance < model.Amount)
        {
            var balance = wallet?.Balance ?? 0M;
            ModelState.AddModelError(string.Empty, $"Insufficient wallet balance. Current balance: {balance:C}.");
            return View(model);
        }

        wallet.Balance -= model.Amount;
        context.WalletTransactions.Add(new WalletTransaction
        {
            WalletId = wallet.Id,
            Amount = model.Amount,
            Type = "Debit",
            Status = "Approved",
            PaymentMethod = "Wallet",
            ReferenceNumber = $"Membership payment - {model.MembershipPlan}",
            CreatedAt = DateTime.UtcNow
        });

        var transaction = new Transaction
        {
            UserId = user.Id,
            Amount = model.Amount,
            MembershipPlan = model.MembershipPlan,
            Notes = model.Notes,
            Status = "Approved",
            PaymentDate = DateTime.UtcNow
        };

        context.Transactions.Add(transaction);

        var startDate = DateTime.UtcNow.Date;
        var endDate = startDate.AddMonths(1);
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
                StartDate = startDate,
                EndDate = endDate,
                Status = "Active",
                MonthlyFee = model.Amount
            });
        }
        else
        {
            membership.PlanName = model.MembershipPlan;
            membership.MonthlyFee = model.Amount;
            membership.StartDate = startDate;
            membership.EndDate = endDate;
            membership.Status = "Active";
        }

        await context.SaveChangesAsync();
        TempData["StatusMessage"] = "Membership paid from wallet and activated.";
        return RedirectToAction(nameof(Transactions));
    }
}
