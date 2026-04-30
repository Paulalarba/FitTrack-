using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace FitTrack.Models;

public class ApplicationUser : IdentityUser
{
    [Required]
    [StringLength(120)]
    public string FullName { get; set; } = string.Empty;

    [StringLength(260)]
    public string? ProfilePicture { get; set; }

    [StringLength(30)]
    public string Role { get; set; } = "User";

    [StringLength(200)]
    public string? Address { get; set; }

    public DateTime JoinedDate { get; set; } = DateTime.UtcNow;

    public ICollection<Transaction> Transactions { get; set; } = [];
    public ICollection<Membership> Memberships { get; set; } = [];
    public Wallet? Wallet { get; set; }
}
