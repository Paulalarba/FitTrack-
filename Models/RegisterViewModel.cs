using System.ComponentModel.DataAnnotations;

namespace FitTrack.Models
{
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Commander name is required")]
        [Display(Name = "Full Name")]
        public string FullName { get; set; }

        [Required(ErrorMessage = "Neural sync email is required")]
        [EmailAddress(ErrorMessage = "Invalid neural link address")]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Access key is required")]
        [DataType(DataType.Password)]
        [MinLength(8, ErrorMessage = "Access key must be at least 8 characters")]
        [Display(Name = "Password")]
        public string Password { get; set; }

        [Required(ErrorMessage = "Please select a primary directive")]
        public string Directive { get; set; } // MuscleGain, WeightLoss, Maintenance
    }
}
