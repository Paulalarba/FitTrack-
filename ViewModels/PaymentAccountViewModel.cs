using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace FitTrack.ViewModels;

public class PaymentAccountViewModel
{
    public int? Id { get; set; }

    [Required]
    [StringLength(120)]
    [Display(Name = "Account Name")]
    public string AccountName { get; set; } = string.Empty;

    [Required]
    [StringLength(80)]
    [Display(Name = "Account Number")]
    public string AccountNumber { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    [Display(Name = "Payment Type")]
    public string PaymentType { get; set; } = "GCash";

    public string? ExistingQrCodePath { get; set; }

    [Display(Name = "QR Code Image")]
    public IFormFile? QrCodeFile { get; set; }
}
