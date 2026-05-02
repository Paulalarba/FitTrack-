using System.ComponentModel.DataAnnotations;

namespace FitTrack.ViewModels;

public class QrScanRequest
{
    [Required]
    public string QrPayload { get; set; } = string.Empty;
}
