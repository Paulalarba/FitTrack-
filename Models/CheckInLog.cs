// ============================================================
// CheckInLog.cs — Gym Entrance Scan Audit Log Model
// ============================================================
// Every time an admin scans a member's QR code at the gym entrance,
// one CheckInLog row is written to the database — regardless of
// whether entry was allowed or denied.
//
// This table serves as a tamper-evident audit trail:
//   - Proves a member was (or wasn't) at the gym at a given time.
//   - Enables the 45-second cooldown check (prevent double-scans).
//   - MemberId is nullable with SetNull delete behavior, so historical
//     scan records are preserved even if the member's account is deleted.
//
// Status values:
//   "Allowed"   — QR valid AND membership active; member enters the gym.
//   "Denied"    — QR invalid OR membership expired; entry refused.
//   "Duplicate" — Re-scan within the cooldown window; no new row written
//                 (handled in memory in AdminController, not persisted).
// ============================================================

using System.ComponentModel.DataAnnotations;

namespace FitTrack.Models;

/// <summary>
/// Represents a single QR code scan event at the gym entrance.
/// One row is inserted for every scan attempt (allowed or denied).
/// </summary>
public class CheckInLog
{
    /// <summary>Auto-incremented primary key for this log entry.</summary>
    public int Id { get; set; }

    /// <summary>
    /// The Id of the member whose QR code was scanned.
    /// Nullable — set to NULL (not deleted) if the member account is removed,
    /// preserving the audit trail for deleted users.
    /// </summary>
    public string? MemberId { get; set; }

    /// <summary>
    /// The UTC date and time when the QR code was scanned.
    /// Defaults to now. Indexed in the database to speed up
    /// "most recent scan" queries used in the cooldown check.
    /// </summary>
    public DateTime CheckInTime { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// The outcome of this scan attempt.
    /// "Allowed" — member entered the gym.
    /// "Denied"  — entry refused (expired membership or invalid QR).
    /// </summary>
    [Required]
    [StringLength(20)]
    public string Status { get; set; } = "Denied";

    /// <summary>
    /// A short human-readable explanation of why the scan was allowed or denied.
    /// Examples: "Membership active", "Membership expired or inactive",
    /// "Invalid or tampered QR code".
    /// </summary>
    [StringLength(160)]
    public string? Reason { get; set; }

    /// <summary>Navigation property — the member whose QR was scanned. May be null.</summary>
    public ApplicationUser? Member { get; set; }
}
