// ============================================================
// Transaction.cs — Membership Payment Transaction Model
// ============================================================
// Records a single membership payment request made by a member.
// Transactions are created in two ways:
//   1. Member pays from wallet → Transaction is auto-approved,
//      membership is activated immediately.
//   2. Manual payment (GCash, bank) → Transaction starts as "Pending"
//      and an admin must approve or reject it via the Admin panel.
//
// Status lifecycle:
//   Pending  → awaiting admin review (manual payments)
//   Approved → payment confirmed; linked Membership set to "Active"
//   Rejected → payment denied; linked Membership set to "Rejected"
// ============================================================

using System.ComponentModel.DataAnnotations;

namespace FitTrack.Models;

/// <summary>
/// Represents a membership payment transaction submitted by a member.
/// Acts as the financial record for one membership billing event.
/// </summary>
public class Transaction
{
    /// <summary>Auto-incremented primary key for this transaction.</summary>
    public int Id { get; set; }

    /// <summary>
    /// Foreign key linking this transaction to the member who submitted it.
    /// References the Id column in AspNetUsers.
    /// </summary>
    [Required]
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// The amount paid in Philippine Pesos (₱).
    /// Must be at least ₱1. Validated against the chosen plan's fixed price
    /// in the MemberController to prevent price manipulation.
    /// </summary>
    [Required]
    [Range(1, 1000000)]
    public decimal Amount { get; set; }

    /// <summary>
    /// The UTC timestamp when this payment was submitted.
    /// Defaults to the current UTC time. Used to sort transactions chronologically.
    /// </summary>
    public DateTime PaymentDate { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// The name of the plan the member subscribed to
    /// (e.g., "Classic Membership" or "PF Black Card Membership").
    /// Copied into the linked Membership record when approved.
    /// </summary>
    [Required]
    [StringLength(50)]
    public string MembershipPlan { get; set; } = string.Empty;

    /// <summary>
    /// The current review status of this transaction.
    /// Possible values: "Pending", "Approved", "Rejected".
    /// Wallet payments skip "Pending" and go directly to "Approved".
    /// </summary>
    [Required]
    [StringLength(20)]
    public string Status { get; set; } = "Pending";

    /// <summary>
    /// Optional free-text note entered by the member when submitting the payment
    /// (e.g., GCash reference number, bank transfer details). Max 300 characters.
    /// </summary>
    [StringLength(300)]
    public string? Notes { get; set; }

    /// <summary>Navigation property — the member who made this payment.</summary>
    public ApplicationUser? User { get; set; }
}
