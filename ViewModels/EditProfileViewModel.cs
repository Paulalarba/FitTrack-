using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace FitTrack.ViewModels;

public class EditProfileViewModel
{
    [Required]
    [StringLength(120)]
    [Display(Name = "Full Name")]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Phone]
    [Display(Name = "Phone Number")]
    public string? PhoneNumber { get; set; }

    [StringLength(200)]
    public string? Address { get; set; }

    [Display(Name = "Profile Photo")]
    public IFormFile? ProfileImage { get; set; }

    public string? CurrentProfilePicture { get; set; }
}
