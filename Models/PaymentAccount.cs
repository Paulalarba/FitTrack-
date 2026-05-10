// ============================================================
// PaymentAccount.cs — Admin-Managed Payment Account Model
// ============================================================
// Represents a GCash, Maya, or bank account that the gym owner
// wants members to send money to when topping up their wallet.
//
// Admins manage these accounts through the Admin → Payment Accounts
// section. Each account has:
//   - A QR code image (uploaded by admin) that members can scan
//     to quickly initiate a payment.
//   - Account name and number for manual transfers.
//   - A payment type label (e.g., "GCash", "Maya", "BDO").
//
// Members can browse these on the Member → Payment Options page,
// scan the QR code, then submit a deposit proof through the wallet.
// ============================================================

using System.ComponentModel.DataAnnotations;

namespace FitTrack.Models;

/// <summary>
/// Represents a payment account (GCash, Maya, bank) displayed to members
/// so they know where to send money when topping up their wallet.
/// </summary>
public class PaymentAccount
{
    /// <summary>Auto-incremented primary key for this payment account.</summary>
    public int Id { get; set; }

    /// <summary>
    /// The display name for this account (e.g., "Juan Dela Cruz", "FitTrack GCash").
    /// Shown to members on the Payment Options page alongside the QR code.
    /// Max 120 characters.
    /// </summary>
    [Required]
    [StringLength(120)]
    public string AccountName { get; set; } = string.Empty;

    /// <summary>
    /// The account number or identifier (e.g., GCash number "09XX-XXX-XXXX",
    /// bank account number, or Maya account). Shown to members for manual transfers.
    /// Max 80 characters.
    /// </summary>
    [Required]
    [StringLength(80)]
    public string AccountNumber { get; set; } = string.Empty;

    /// <summary>
    /// The type or provider of this payment account.
    /// Examples: "GCash", "Maya", "BPI", "BDO", "UnionBank".
    /// Indexed in the database (see DbContext) to support grouped display.
    /// Max 50 characters.
    /// </summary>
    [Required]
    [StringLength(50)]
    public string PaymentType { get; set; } = string.Empty;

    /// <summary>
    /// Relative URL path to the uploaded QR code image,
    /// e.g., "/uploads/payment-accounts/abc123.png".
    /// The image is displayed to members so they can scan it with their banking app.
    /// Must be a JPG, PNG, or WEBP file (enforced in AdminController.ValidateQrCode).
    /// Max 260 characters.
    /// </summary>
    [Required]
    [StringLength(260)]
    public string QrCodePath { get; set; } = string.Empty;

    /// <summary>
    /// UTC timestamp when this payment account was first added by an admin.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// UTC timestamp of the most recent edit to this account. Null if never edited.
    /// Updated in AdminController.EditPaymentAccount() on every save.
    /// </summary>
    public DateTime? UpdatedAt { get; set; }
}
