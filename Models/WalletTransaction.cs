using System.ComponentModel.DataAnnotations;

namespace FitTrack.Models;

public class WalletTransaction
{
    public int Id { get; set; }

    public int WalletId { get; set; }

    [Range(1, 1000000)]
    public decimal Amount { get; set; }

    [Required]
    [StringLength(20)]
    public string Type { get; set; } = "Credit";

    [Required]
    [StringLength(20)]
    public string Status { get; set; } = "Pending";

    [Required]
    [StringLength(30)]
    public string PaymentMethod { get; set; } = "Online";

    [StringLength(120)]
    public string? ReferenceNumber { get; set; }

    [StringLength(260)]
    public string? ProofPath { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Wallet? Wallet { get; set; }
}
