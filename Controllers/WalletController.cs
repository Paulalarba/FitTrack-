using FitTrack.Data;
using FitTrack.Models;
using FitTrack.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FitTrack.Controllers;

[Authorize]
public class WalletController(
    ApplicationDbContext context,
    UserManager<ApplicationUser> userManager,
    IWebHostEnvironment environment) : Controller
{
    private static readonly HashSet<string> AllowedProofExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg",
        ".jpeg",
        ".png",
        ".pdf"
    };

    public async Task<IActionResult> Index()
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null)
        {
            return RedirectToAction("Login", "Account");
        }

        var wallet = await GetOrCreateWalletAsync(user.Id);
        var transactions = await context.WalletTransactions
            .Where(t => t.WalletId == wallet.Id)
            .OrderByDescending(t => t.CreatedAt)
            .Take(10)
            .ToListAsync();

        return View(new WalletDashboardViewModel
        {
            Balance = wallet.Balance,
            TotalDeposits = await context.WalletTransactions
                .Where(t => t.WalletId == wallet.Id && t.Type == "Credit" && t.Status == "Approved")
                .SumAsync(t => (decimal?)t.Amount) ?? 0M,
            RecentTransactions = transactions
        });
    }

    [HttpGet]
    public IActionResult Deposit() => View(new DepositFundsViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Deposit(DepositFundsViewModel model)
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null)
        {
            return RedirectToAction("Login", "Account");
        }

        if (model.PaymentMethod != "Online" && model.PaymentMethod != "In-person")
        {
            ModelState.AddModelError(nameof(model.PaymentMethod), "Select a valid payment method.");
        }

        if (model.ProofFile is not null)
        {
            var extension = Path.GetExtension(model.ProofFile.FileName);
            if (!AllowedProofExtensions.Contains(extension))
            {
                ModelState.AddModelError(nameof(model.ProofFile), "Proof must be a JPG, PNG, or PDF file.");
            }
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var wallet = await GetOrCreateWalletAsync(user.Id);
        var proofPath = await SaveProofFileAsync(model.ProofFile);

        context.WalletTransactions.Add(new WalletTransaction
        {
            WalletId = wallet.Id,
            Amount = model.Amount,
            Type = "Credit",
            Status = "Pending",
            PaymentMethod = model.PaymentMethod,
            ReferenceNumber = model.ReferenceNumber,
            ProofPath = proofPath,
            CreatedAt = DateTime.UtcNow
        });

        await context.SaveChangesAsync();
        TempData["StatusMessage"] = "Deposit request submitted for admin approval.";
        return RedirectToAction(nameof(Index));
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

    private async Task<string?> SaveProofFileAsync(IFormFile? file)
    {
        if (file is null || file.Length == 0)
        {
            return null;
        }

        var uploadsPath = Path.Combine(environment.WebRootPath, "uploads", "wallet-proofs");
        Directory.CreateDirectory(uploadsPath);

        var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
        var fullPath = Path.Combine(uploadsPath, fileName);

        await using var stream = System.IO.File.Create(fullPath);
        await file.CopyToAsync(stream);

        return $"/uploads/wallet-proofs/{fileName}";
    }
}
