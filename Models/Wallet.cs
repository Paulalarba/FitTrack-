// ============================================================
// Wallet.cs — Member Digital Wallet Model
// ============================================================
// Each member has exactly one wallet (one-to-one with ApplicationUser).
// The wallet holds a peso balance that members can:
//   1. Top up (deposit) via GCash/bank — admin must approve the deposit.
//   2. Use to pay for memberships directly (instant approval).
//   3. Transfer to another member's wallet (peer-to-peer transfer).
//
// The Balance is always >= 0. Negative balances are prevented by
// the balance-check logic in MemberController and WalletController.
// ============================================================

using System.ComponentModel.DataAnnotations;

namespace FitTrack.Models;

/// <summary>
/// Represents a member's digital wallet that holds a peso balance.
/// Created automatically the first time a user visits the Wallet page
/// (via GetOrCreateWalletAsync in the controllers).
/// </summary>
public class Wallet
{
    /// <summary>Auto-incremented primary key for this wallet record.</summary>
    public int Id { get; set; }

    /// <summary>
    /// Foreign key linking this wallet to its owner in AspNetUsers.
    /// Enforced as unique at the DB level — a user can only have one wallet.
    /// </summary>
    [Required]
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// The current peso balance available in this wallet.
    /// Range validated 0–1,000,000. Stored with 18,2 precision (see DbContext).
    /// Updated atomically in database transactions to prevent race conditions.
    /// </summary>
    [Range(0, 1000000)]
    public decimal Balance { get; set; }

    /// <summary>Navigation property — the user who owns this wallet.</summary>
    public ApplicationUser? User { get; set; }

    /// <summary>
    /// All credit and debit movements recorded against this wallet.
    /// Used to display the transaction history on the Wallet dashboard.
    /// </summary>
    public ICollection<WalletTransaction> WalletTransactions { get; set; } = [];
}
