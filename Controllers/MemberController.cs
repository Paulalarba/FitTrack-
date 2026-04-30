using FitTrack.Data;
using FitTrack.Models;
using FitTrack.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace FitTrack.Controllers;

[Authorize]
public class MemberController(
    ApplicationDbContext context,
    UserManager<ApplicationUser> userManager,
    ILogger<MemberController> logger) : Controller
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
    public async Task<IActionResult> TransferFunds()
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null)
        {
            return RedirectToAction("Login", "Account");
        }

        return View(new TransferFundsViewModel
        {
            WalletBalance = await GetWalletBalanceAsync(user.Id)
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TransferFunds(TransferFundsViewModel model)
    {
        var sender = await userManager.GetUserAsync(User);
        if (sender is null)
        {
            return RedirectToAction("Login", "Account");
        }

        model.WalletBalance = await GetWalletBalanceAsync(sender.Id);
        var recipient = await FindUserByPhoneAsync(model.RecipientPhoneNumber);

        if (recipient is null)
        {
            ModelState.AddModelError(nameof(model.RecipientPhoneNumber), "No member account was found with that phone number.");
        }
        else if (recipient.Id == sender.Id)
        {
            ModelState.AddModelError(nameof(model.RecipientPhoneNumber), "You cannot transfer funds to yourself.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        await using var dbTransaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable);

        var senderWallet = await GetOrCreateWalletAsync(sender.Id);
        var recipientWallet = await GetOrCreateWalletAsync(recipient!.Id);

        if (senderWallet.Balance < model.Amount)
        {
            ModelState.AddModelError(nameof(model.Amount), $"Insufficient wallet balance. Current balance: {senderWallet.Balance:C}.");
            model.WalletBalance = senderWallet.Balance;
            await dbTransaction.RollbackAsync();
            return View(model);
        }

        senderWallet.Balance -= model.Amount;
        recipientWallet.Balance += model.Amount;

        var now = DateTime.UtcNow;
        var senderReference = BuildTransferReference("To", recipient.FullName, recipient.PhoneNumber, model.Note);
        var recipientReference = BuildTransferReference("From", sender.FullName, sender.PhoneNumber, model.Note);

        context.WalletTransactions.AddRange(
            new WalletTransaction
            {
                WalletId = senderWallet.Id,
                Amount = model.Amount,
                Type = "Debit",
                Status = "Approved",
                PaymentMethod = "Wallet Transfer",
                ReferenceNumber = senderReference,
                CreatedAt = now
            },
            new WalletTransaction
            {
                WalletId = recipientWallet.Id,
                Amount = model.Amount,
                Type = "Credit",
                Status = "Approved",
                PaymentMethod = "Wallet Transfer",
                ReferenceNumber = recipientReference,
                CreatedAt = now
            });

        await context.SaveChangesAsync();
        await dbTransaction.CommitAsync();

        logger.LogInformation(
            "Wallet transfer completed. SenderUserId={SenderUserId}, RecipientUserId={RecipientUserId}, Amount={Amount}",
            sender.Id,
            recipient.Id,
            model.Amount);

        TempData["StatusMessage"] = $"Transfer of {model.Amount:C} to {recipient.FullName} completed.";
        return RedirectToAction("Index", "Wallet");
    }

    [HttpGet]
    public async Task<IActionResult> LookupRecipient(string phoneNumber)
    {
        var currentUser = await userManager.GetUserAsync(User);
        if (currentUser is null)
        {
            return Unauthorized();
        }

        var recipient = await FindUserByPhoneAsync(phoneNumber);
        if (recipient is null)
        {
            return Json(new { found = false });
        }

        return Json(new
        {
            found = true,
            isSelf = recipient.Id == currentUser.Id,
            fullName = recipient.FullName
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

    private async Task<decimal> GetWalletBalanceAsync(string userId)
    {
        return await context.Wallets
            .Where(w => w.UserId == userId)
            .Select(w => (decimal?)w.Balance)
            .FirstOrDefaultAsync() ?? 0M;
    }

    private async Task<Wallet> GetOrCreateWalletAsync(string userId)
    {
        var wallet = await context.Wallets.FirstOrDefaultAsync(w => w.UserId == userId);
        if (wallet is not null)
        {
            return wallet;
        }

        wallet = new Wallet { UserId = userId, Balance = 0M };
        context.Wallets.Add(wallet);
        await context.SaveChangesAsync();
        return wallet;
    }

    private async Task<ApplicationUser?> FindUserByPhoneAsync(string? phoneNumber)
    {
        var normalizedPhone = NormalizePhone(phoneNumber);
        if (string.IsNullOrWhiteSpace(normalizedPhone))
        {
            return null;
        }

        var users = await context.Users
            .Where(u => u.PhoneNumber != null)
            .ToListAsync();

        return users.FirstOrDefault(u => NormalizePhone(u.PhoneNumber) == normalizedPhone);
    }

    private static string NormalizePhone(string? phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
        {
            return string.Empty;
        }

        return new string(phoneNumber.Where(char.IsDigit).ToArray());
    }

    private static string BuildTransferReference(string direction, string fullName, string? phoneNumber, string? note)
    {
        var reference = $"{direction}: {fullName} ({phoneNumber})";
        if (!string.IsNullOrWhiteSpace(note))
        {
            reference += $" - {note.Trim()}";
        }

        return reference.Length <= 120 ? reference : reference[..120];
    }
}
