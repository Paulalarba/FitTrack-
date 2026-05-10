// ============================================================
// WalletTransaction.cs — Wallet Credit / Debit Movement Model
// ============================================================
// Records every financial movement in a member's wallet.
// Each row represents a single credit (money in) or debit (money out).
//
// Type values:
//   "Credit" — money added to the wallet (deposit request from member)
//   "Debit"  — money deducted from the wallet (membership payment or transfer)
//
// Status values:
//   "Pending"  — Credit requests awaiting admin approval
//   "Approved" — Processed credits; the wallet balance has been updated
//   "Rejected" — Deposit denied by admin; balance is NOT updated
//
// PaymentMethod values:
//   "Online"           — GCash, Maya, bank transfer (upload proof)
//   "In-person"        — Cash paid at the gym counter
//   "Wallet"           — Internal debit when paying for membership via wallet
//   "Wallet Transfer"  — Peer-to-peer transfer between members
// ============================================================

using System.ComponentModel.DataAnnotations;

namespace FitTrack.Models;

/// <summary>
/// Represents a single credit or debit movement within a member's wallet.
/// Think of this as a bank statement line — one row per financial event.
/// </summary>
public class WalletTransaction
{
    /// <summary>Auto-incremented primary key for this wallet transaction.</summary>
    public int Id { get; set; }

    /// <summary>
    /// Foreign key linking this record to the wallet it belongs to.
    /// Cascade-deleted when the wallet is removed.
    /// </summary>
    public int WalletId { get; set; }

    /// <summary>
    /// The peso amount of this transaction (always positive, >= ₱1).
    /// Whether it's a credit or debit is determined by the <see cref="Type"/> field.
    /// </summary>
    [Range(1, 1000000)]
    public decimal Amount { get; set; }

    /// <summary>
    /// Direction of the transaction: "Credit" (money in) or "Debit" (money out).
    /// </summary>
    [Required]
    [StringLength(20)]
    public string Type { get; set; } = "Credit";

    /// <summary>
    /// Processing status of this transaction.
    /// Credits start as "Pending" until an admin approves or rejects them.
    /// Debits (membership payments, transfers) are created as "Approved" immediately.
    /// </summary>
    [Required]
    [StringLength(20)]
    public string Status { get; set; } = "Pending";

    /// <summary>
    /// How the money was deposited or moved.
    /// Examples: "Online", "In-person", "Wallet", "Wallet Transfer".
    /// </summary>
    [Required]
    [StringLength(30)]
    public string PaymentMethod { get; set; } = "Online";

    /// <summary>
    /// Optional reference number provided by the member as proof of payment
    /// (e.g., GCash transaction ID, bank reference code).
    /// For peer-to-peer transfers this is auto-generated as "To/From: Name (Phone)".
    /// </summary>
    [StringLength(120)]
    public string? ReferenceNumber { get; set; }

    /// <summary>
    /// Relative URL path to the uploaded payment proof image/PDF,
    /// e.g., "/uploads/wallet-proofs/abc123.jpg".
    /// Null for wallet-to-wallet transfers (no proof required).
    /// </summary>
    [StringLength(260)]
    public string? ProofPath { get; set; }

    /// <summary>
    /// UTC timestamp when this transaction record was created.
    /// Used to sort the wallet history in chronological order.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Navigation property — the wallet this transaction belongs to.</summary>
    public Wallet? Wallet { get; set; }
}
