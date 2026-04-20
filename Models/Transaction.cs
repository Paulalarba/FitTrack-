using System.ComponentModel.DataAnnotations;

namespace FitTrack.Models
{
    public class Transaction
    {
        public int Id { get; set; }
        [Required]
        public string UserId { get; set; } // Foreign Key to ApplicationUser
        [Required]
        public decimal Amount { get; set; }
        public DateTime PaymentDate { get; set; } = DateTime.Now;
        public string MembershipPlan { get; set; } // e.g., "Monthly", "Annual"
        public string Status { get; set; } // Paid, Pending, Failed
    }
}
