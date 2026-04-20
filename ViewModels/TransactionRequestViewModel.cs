using System.ComponentModel.DataAnnotations;

namespace FitTrack.ViewModels;

public class TransactionRequestViewModel
{
    [Required]
    [Display(Name = "Membership Plan")]
    public string MembershipPlan { get; set; } = "Monthly";

    [Required]
    [Range(1, 1000000)]
    public decimal Amount { get; set; }

    [StringLength(300)]
    public string? Notes { get; set; }
}
