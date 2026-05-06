// ============================================================
// WalletController.cs — Member Wallet Controller
// ============================================================
// Handles all wallet-related pages for logged-in members:
//
//   Index   — Shows the wallet dashboard: current balance, total deposits,
//             and the 10 most recent wallet transactions.
//
//   Deposit — Allows a member to request a balance top-up by uploading
//             proof of payment (GCash receipt, bank screenshot, or PDF).
//             The request is saved as "Pending" and awaits admin approval
//             in Admin → Wallet Requests before the balance is credited.
//
// Peer-to-peer transfers live in MemberController.TransferFunds
// because they also deal with memberships and cross-user logic.
//
// All actions require [Authorize] — unauthenticated users are redirected
// to /Account/Login automatically by the Identity middleware.
// ============================================================

using FitTrack.Data;
using FitTrack.Models;
using FitTrack.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FitTrack.Controllers;

/// <summary>
/// Handles wallet balance display and deposit requests for authenticated members.
/// </summary>
[Authorize] // All actions in this controller require the user to be logged in.
public class WalletController(
    ApplicationDbContext context,
    UserManager<ApplicationUser> userManager,
    IWebHostEnvironment environment) : Controller
{
    // Allowed file extensions for payment proof uploads.
    // PDF is accepted in addition to images since bank transfers often produce PDF receipts.
    private static readonly HashSet<string> AllowedProofExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg",
        ".jpeg",
        ".png",
        ".pdf"
    };

    // ── Wallet Dashboard ──────────────────────────────────────────────────────

    /// <summary>
    /// GET /Wallet
    /// Displays the member's wallet overview: balance, total approved deposits,
    /// and the 10 most recent wallet transactions.
    ///
    /// GetOrCreateWalletAsync ensures a Wallet row exists for the user before
    /// querying — this lazily creates the wallet the first time it is accessed.
    /// </summary>
    public async Task<IActionResult> Index()
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null)
        {
            return RedirectToAction("Login", "Account");
        }

        // Retrieve the wallet (or create one if this is the first visit).
        var wallet = await GetOrCreateWalletAsync(user.Id);

        // Load the 10 most recent transactions for the history feed.
        var transactions = await context.WalletTransactions
            .Where(t => t.WalletId == wallet.Id)
            .OrderByDescending(t => t.CreatedAt)
            .Take(10)
            .ToListAsync();

        // Sum only APPROVED credits to show true total deposited money.
        return View(new WalletDashboardViewModel
        {
            Balance = wallet.Balance,
            TotalDeposits = await context.WalletTransactions
                .Where(t => t.WalletId == wallet.Id && t.Type == "Credit" && t.Status == "Approved")
                .SumAsync(t => (decimal?)t.Amount) ?? 0M, // Null-coalesce handles empty result set.
            RecentTransactions = transactions
        });
    }

    // ── Deposit ───────────────────────────────────────────────────────────────

    /// <summary>
    /// GET /Wallet/Deposit
    /// Displays the deposit request form with an empty <see cref="DepositFundsViewModel"/>.
    /// </summary>
    [HttpGet]
    public IActionResult Deposit() => View(new DepositFundsViewModel());

    /// <summary>
    /// POST /Wallet/Deposit
    /// Submits a deposit request that an admin must approve before the balance updates.
    ///
    /// Flow:
    ///   1. Validate the payment method (must be "Online" or "In-person").
    ///   2. Validate the proof file extension if one was uploaded.
    ///   3. Save the proof file to wwwroot/uploads/wallet-proofs/ (if provided).
    ///   4. Create a WalletTransaction with Type="Credit" and Status="Pending".
    ///   5. Redirect to the wallet dashboard with a success message.
    ///
    /// The wallet balance is NOT changed here — it is only updated in
    /// AdminController.ApproveDeposit() after admin review.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Deposit(DepositFundsViewModel model)
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null)
        {
            return RedirectToAction("Login", "Account");
        }

        // Validate payment method — only accepted values are "Online" or "In-person".
        if (model.PaymentMethod != "Online" && model.PaymentMethod != "In-person")
        {
            ModelState.AddModelError(nameof(model.PaymentMethod), "Select a valid payment method.");
        }

        // Validate proof file extension if the member chose to upload one.
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

        // Ensure the user's wallet exists (creates one if needed).
        var wallet = await GetOrCreateWalletAsync(user.Id);

        // Save the proof file to disk and get the web-accessible relative path.
        var proofPath = await SaveProofFileAsync(model.ProofFile);

        // Record the pending deposit request — balance is NOT updated yet.
        context.WalletTransactions.Add(new WalletTransaction
        {
            WalletId = wallet.Id,
            Amount = model.Amount,
            Type = "Credit",         // This is incoming money.
            Status = "Pending",      // Awaiting admin approval.
            PaymentMethod = model.PaymentMethod,
            ReferenceNumber = model.ReferenceNumber,
            ProofPath = proofPath,
            CreatedAt = DateTime.UtcNow
        });

        await context.SaveChangesAsync();
        TempData["StatusMessage"] = "Deposit request submitted for admin approval.";
        return RedirectToAction(nameof(Index));
    }

    // ── Private Helpers ───────────────────────────────────────────────────────

    /// <summary>
    /// Retrieves the wallet for <paramref name="userId"/>, or creates a new one
    /// with a zero balance if none exists yet.
    /// This is the "lazy wallet creation" pattern — we don't create wallets on sign-up,
    /// only when the user first interacts with the wallet feature.
    /// </summary>
    private async Task<Wallet> GetOrCreateWalletAsync(string userId)
    {
        var wallet = await context.Wallets.FirstOrDefaultAsync(w => w.UserId == userId);
        if (wallet is not null)
        {
            return wallet;
        }

        // First visit — create the wallet and persist it immediately.
        wallet = new Wallet { UserId = userId, Balance = 0M };
        context.Wallets.Add(wallet);
        await context.SaveChangesAsync();
        return wallet;
    }

    /// <summary>
    /// Saves an uploaded proof file to the wwwroot/uploads/wallet-proofs/ directory.
    /// Returns the web-accessible relative path (e.g., "/uploads/wallet-proofs/abc.jpg"),
    /// or <c>null</c> if no file was uploaded.
    ///
    /// A GUID is used as the filename to prevent:
    ///   1. Filename collisions between multiple users uploading similarly-named files.
    ///   2. Path traversal attacks (no user-controlled path component).
    /// </summary>
    private async Task<string?> SaveProofFileAsync(IFormFile? file)
    {
        // Return null if the member didn't attach a proof file.
        if (file is null || file.Length == 0)
        {
            return null;
        }

        var uploadsPath = Path.Combine(environment.WebRootPath, "uploads", "wallet-proofs");
        Directory.CreateDirectory(uploadsPath); // Create directory if it doesn't exist.

        var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
        var fullPath = Path.Combine(uploadsPath, fileName);

        // Stream the file to disk without loading it all into memory.
        await using var stream = System.IO.File.Create(fullPath);
        await file.CopyToAsync(stream);

        return $"/uploads/wallet-proofs/{fileName}";
    }
}
