using System.ComponentModel.DataAnnotations;

namespace FitTrack.Models
{
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Full Name is required")]
        [Display(Name = "Full Name")]
        public string FullName { get; set; }

        [Required(ErrorMessage = "Email is required")]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Password is required")]
        [DataType(DataType.Password)]
        [MinLength(8, ErrorMessage = "Password must be at least 8 characters")]
        [Display(Name = "Password")]
        public string Password { get; set; }

    }
}
