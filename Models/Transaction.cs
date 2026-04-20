using System.ComponentModel.DataAnnotations;

namespace FitTrack.Models;

public class Transaction
{
    public int Id { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;

    [Required]
    [Range(1, 1000000)]
    public decimal Amount { get; set; }

    public DateTime PaymentDate { get; set; } = DateTime.UtcNow;

    [Required]
    [StringLength(50)]
    public string MembershipPlan { get; set; } = string.Empty;

    [Required]
    [StringLength(20)]
    public string Status { get; set; } = "Pending";

    [StringLength(300)]
    public string? Notes { get; set; }

    public ApplicationUser? User { get; set; }
}
