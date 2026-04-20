using FitTrack.Models;

namespace FitTrack.ViewModels;

public class MemberTransactionsViewModel
{
    public List<Transaction> Transactions { get; set; } = [];
    public int Page { get; set; }
    public int TotalPages { get; set; }
}
