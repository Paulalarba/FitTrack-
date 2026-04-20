using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FitTrack.Models;

public class Membership
{
    public int Id { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string PlanName { get; set; } = string.Empty;

    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }

    [Required]
    [StringLength(20)]
    public string Status { get; set; } = "Pending";

    [Range(0, 1000000)]
    public decimal MonthlyFee { get; set; }

    public ApplicationUser? User { get; set; }

    [NotMapped]
    public bool IsActive => Status == "Active" && EndDate >= DateTime.UtcNow.Date;
}
