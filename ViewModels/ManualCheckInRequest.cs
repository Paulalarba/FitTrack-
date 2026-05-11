using System.ComponentModel.DataAnnotations;

namespace FitTrack.ViewModels;

/// <summary>
/// Request body for POST /Admin/ManualCheckIn.
/// Used when the admin performs a name-based check-in as a fallback
/// when the QR scanner is unavailable or the member has no QR code.
/// </summary>
public class ManualCheckInRequest
{
    /// <summary>The Identity GUID of the member to check in.</summary>
    [Required]
    public string MemberId { get; set; } = string.Empty;
}
