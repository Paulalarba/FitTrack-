using FitTrack.Models;

namespace FitTrack.ViewModels;

public class WalletDashboardViewModel
{
    public decimal Balance { get; set; }
    public decimal TotalDeposits { get; set; }
    public List<WalletTransaction> RecentTransactions { get; set; } = [];
}
