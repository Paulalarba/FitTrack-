using FitTrack.Models;

namespace FitTrack.ViewModels;

public class AdminDashboardViewModel
{
    public int TotalUsers { get; set; }
    public int TotalTransactions { get; set; }
    public int ActiveMemberships { get; set; }
    public List<ApplicationUser> RecentUsers { get; set; } = [];
    public List<Transaction> RecentTransactions { get; set; } = [];

    // Notification Widget Data
    public int PendingPaymentsCount { get; set; }
    public int PendingWalletRequestsCount { get; set; }
    public int NewUsersTodayCount { get; set; }
}
