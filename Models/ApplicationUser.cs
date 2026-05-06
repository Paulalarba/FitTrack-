// ============================================================
// ApplicationUser.cs — Custom Identity User Model
// ============================================================
// Extends ASP.NET Core's built-in IdentityUser to add FitTrack-
// specific properties. IdentityUser already provides: Id, Email,
// UserName, PasswordHash, PhoneNumber, SecurityStamp, etc.
//
// EF Core stores this in the "AspNetUsers" table (Identity default).
// The extra columns below are added alongside the Identity columns
// via EF Core migrations.
// ============================================================

using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace FitTrack.Models;

/// <summary>
/// The application's user entity. Extends <see cref="IdentityUser"/> with
/// FitTrack-specific fields like full name, profile picture, and QR token.
/// </summary>
public class ApplicationUser : IdentityUser
{
    /// <summary>
    /// The member's full display name (e.g., "Juan Dela Cruz").
    /// Required; max 120 characters. Shown in dashboard headers, admin lists, etc.
    /// </summary>
    [Required]
    [StringLength(120)]
    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// Relative URL path to the member's uploaded profile picture,
    /// e.g., "/uploads/profiles/abc123.jpg".
    /// Null if the user has not uploaded a photo yet.
    /// </summary>
    [StringLength(260)]
    public string? ProfilePicture { get; set; }

    /// <summary>
    /// The user's role label stored as a plain string ("Admin" or "User").
    /// This mirrors the Identity role assigned via UserManager.AddToRoleAsync()
    /// and is kept in sync to make role-aware UI rendering simpler.
    /// </summary>
    [StringLength(30)]
    public string Role { get; set; } = "User";

    /// <summary>
    /// The member's home or billing address. Optional; max 200 characters.
    /// </summary>
    [StringLength(200)]
    public string? Address { get; set; }

    /// <summary>
    /// A cryptographically-random, URL-safe token embedded inside the member's QR code.
    /// When a member presses "Regenerate QR", a new token is generated and the old one
    /// is immediately invalidated — previously printed or saved QR codes stop working.
    /// Enforced as unique at the DB level (see ApplicationDbContext).
    /// </summary>
    [StringLength(128)]
    public string? QrCodeToken { get; set; }

    /// <summary>
    /// The UTC date-time when this account was registered.
    /// Defaults to the current UTC time at object creation.
    /// Used to sort "Recently Joined" users on the Admin Dashboard.
    /// </summary>
    public DateTime JoinedDate { get; set; } = DateTime.UtcNow;

    // ── Navigation Properties ─────────────────────────────────────────────────
    // EF Core uses these to perform JOIN queries (e.g., Include(u => u.Transactions)).
    // They are not stored as columns — they are loaded on demand.

    /// <summary>All membership payment transactions made by this user.</summary>
    public ICollection<Transaction> Transactions { get; set; } = [];

    /// <summary>All membership records (active, expired, pending) for this user.</summary>
    public ICollection<Membership> Memberships { get; set; } = [];

    /// <summary>All gym entrance scan events associated with this user's QR code.</summary>
    public ICollection<CheckInLog> CheckInLogs { get; set; } = [];

    /// <summary>The user's digital wallet (one-to-one relationship). May be null before first deposit.</summary>
    public Wallet? Wallet { get; set; }
}
