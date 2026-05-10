using FitTrack.Models;

namespace FitTrack.ViewModels;

public class MemberTransactionItem
{
    public string Type { get; set; } = string.Empty; // "Membership", "Deposit", "Transfer"
    public decimal Amount { get; set; }
    public DateTime Date { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? Reference { get; set; }
    public bool IsDebit { get; set; }
}

public class MemberTransactionsViewModel
{
    public List<MemberTransactionItem> Transactions { get; set; } = [];
    public int Page { get; set; }
    public int TotalPages { get; set; }
}
