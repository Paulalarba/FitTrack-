using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace FitTrack.Models
{
    public class ApplicationUser : IdentityUser
    {
        [Required]
        public string FullName { get; set; }
        public string? ProfilePicture { get; set; }
        public string Role { get; set; } // Admin or User
        public DateTime JoinedDate { get; set; } = DateTime.Now;
    }
}
