using FitTrack.Models;

namespace FitTrack.ViewModels;

public class MemberQrViewModel
{
    public string FullName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? ProfilePicture { get; set; }
    public string QrPayload { get; set; } = string.Empty;
    public Membership? CurrentMembership { get; set; }
    public CheckInLog? LastCheckIn { get; set; }
}
