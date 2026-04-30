using FitTrack.Models;

namespace FitTrack.ViewModels;

public class MemberDashboardViewModel
{
    public ApplicationUser User { get; set; } = default!;
    public Membership? CurrentMembership { get; set; }
    public decimal WalletBalance { get; set; }
    public List<Transaction> RecentTransactions { get; set; } = [];
}
