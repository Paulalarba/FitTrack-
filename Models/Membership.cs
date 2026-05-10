// ============================================================
// Membership.cs — Gym Membership Record Model
// ============================================================
// Represents a single membership subscription for a user.
// A user can have multiple membership records over time
// (one per billing cycle), but only the most recent one is
// considered the "current" membership.
//
// Status lifecycle:
//   Pending  → created when payment is submitted but not yet approved
//   Active   → set by admin approval or wallet payment; grants gym access
//   Rejected → set when admin rejects the payment transaction
//   (Expired) → implied when Status == "Active" but EndDate < today
// ============================================================

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FitTrack.Models;

/// <summary>
/// Represents a gym membership subscription record for a member.
/// Created automatically when a payment transaction is approved.
/// </summary>
public class Membership
{
    /// <summary>Auto-incremented primary key for this membership record.</summary>
    public int Id { get; set; }

    /// <summary>
    /// Foreign key linking this membership to the member's account in AspNetUsers.
    /// Cascade-deleted when the user account is removed.
    /// </summary>
    [Required]
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// The name of the membership tier the user subscribed to.
    /// Allowed values (enforced in controller): "Classic Membership", "PF Black Card Membership".
    /// </summary>
    [Required]
    [StringLength(50)]
    public string PlanName { get; set; } = string.Empty;

    /// <summary>
    /// The UTC date the membership period begins.
    /// Set to today's date when a payment is approved.
    /// </summary>
    public DateTime StartDate { get; set; }

    /// <summary>
    /// The UTC date the membership period expires.
    /// Calculated as StartDate + 1 month (monthly plans) or + 1 year (annual plans).
    /// </summary>
    public DateTime EndDate { get; set; }

    /// <summary>
    /// The current lifecycle status of this membership record.
    /// Possible values: "Pending", "Active", "Rejected".
    /// Note: "Expired" is implied at runtime via the <see cref="IsActive"/> computed property.
    /// </summary>
    [Required]
    [StringLength(20)]
    public string Status { get; set; } = "Pending";

    /// <summary>
    /// The amount the member paid for this membership period (in Philippine Pesos).
    /// Stored with 18-digit precision and 2 decimal places (see ApplicationDbContext).
    /// </summary>
    [Range(0, 1000000)]
    public decimal MonthlyFee { get; set; }

    /// <summary>Navigation property — the user this membership belongs to.</summary>
    public ApplicationUser? User { get; set; }

    /// <summary>
    /// Computed (not stored in DB) property that determines if the membership
    /// currently grants gym access.
    ///
    /// Returns <c>true</c> only if:
    ///   - Status is exactly "Active" (not Pending or Rejected), AND
    ///   - EndDate is today or in the future (membership hasn't expired).
    ///
    /// [NotMapped] tells EF Core to ignore this property — it is never written to the DB.
    /// </summary>
    [NotMapped]
    public bool IsActive => Status == "Active" && EndDate >= DateTime.UtcNow.Date;
}
