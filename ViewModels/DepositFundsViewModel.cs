using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace FitTrack.ViewModels;

public class DepositFundsViewModel : IValidatableObject
{
    [Required]
    [Range(1, 1000000)]
    public decimal Amount { get; set; }

    [Required]
    [Display(Name = "Payment Method")]
    public string PaymentMethod { get; set; } = "Online";

    [StringLength(120)]
    [Display(Name = "Reference Number")]
    public string? ReferenceNumber { get; set; }

    [Display(Name = "Proof of Payment")]
    public IFormFile? ProofFile { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (PaymentMethod == "Online" && string.IsNullOrWhiteSpace(ReferenceNumber))
        {
            yield return new ValidationResult(
                "Reference number is required for online deposits.",
                [nameof(ReferenceNumber)]);
        }
    }
}
