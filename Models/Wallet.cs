using System.ComponentModel.DataAnnotations;

namespace FitTrack.Models;

public class Wallet
{
    public int Id { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;

    [Range(0, 1000000)]
    public decimal Balance { get; set; }

    public ApplicationUser? User { get; set; }
    public ICollection<WalletTransaction> WalletTransactions { get; set; } = [];
}
