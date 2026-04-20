using FitTrack.Models;

namespace FitTrack.ViewModels;

public class AdminTransactionsViewModel
{
    public List<Transaction> Transactions { get; set; } = [];
    public string? SearchTerm { get; set; }
    public string? StatusFilter { get; set; }
    public int Page { get; set; }
    public int TotalPages { get; set; }
}
