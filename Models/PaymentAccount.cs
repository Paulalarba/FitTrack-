using System.ComponentModel.DataAnnotations;

namespace FitTrack.Models;

public class PaymentAccount
{
    public int Id { get; set; }

    [Required]
    [StringLength(120)]
    public string AccountName { get; set; } = string.Empty;

    [Required]
    [StringLength(80)]
    public string AccountNumber { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string PaymentType { get; set; } = string.Empty;

    [Required]
    [StringLength(260)]
    public string QrCodePath { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
