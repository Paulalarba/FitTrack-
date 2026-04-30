using System.ComponentModel.DataAnnotations;

namespace FitTrack.ViewModels;

public class TransferFundsViewModel
{
    [Required]
    [Phone]
    [Display(Name = "Recipient Phone Number")]
    public string RecipientPhoneNumber { get; set; } = string.Empty;

    [Required]
    [Range(1, 100000, ErrorMessage = "Transfer amount must be between {1} and {2}.")]
    public decimal Amount { get; set; }

    [StringLength(80)]
    public string? Note { get; set; }

    public decimal WalletBalance { get; set; }
}
