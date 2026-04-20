using System.ComponentModel.DataAnnotations;

namespace FitTrack.ViewModels;

public class ManageUserViewModel
{
    public string? Id { get; set; }

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

    [Required]
    public string Role { get; set; } = "User";

    [DataType(DataType.Password)]
    public string? Password { get; set; }
}
