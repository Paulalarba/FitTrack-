using System.ComponentModel.DataAnnotations;

namespace FitTrack.Models;

public class CheckInLog
{
    public int Id { get; set; }

    public string? MemberId { get; set; }

    public DateTime CheckInTime { get; set; } = DateTime.UtcNow;

    [Required]
    [StringLength(20)]
    public string Status { get; set; } = "Denied";

    [StringLength(160)]
    public string? Reason { get; set; }

    public ApplicationUser? Member { get; set; }
}
