namespace FitTrack.ViewModels;

public class AdminUsersViewModel
{
    public List<AdminUserListItemViewModel> Users { get; set; } = [];
    public string? SearchTerm { get; set; }
    public int Page { get; set; }
    public int TotalPages { get; set; }
}
