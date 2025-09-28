using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ServiceRequestApp.Models
{
    public class PaymentTransaction
    {
        public int Id { get; set; }
        
        [Required]
        public int ServiceRequestId { get; set; }
        public virtual ServiceRequest ServiceRequest { get; set; }
        
        [Required]
        public string UserId { get; set; }
        public virtual ApplicationUser User { get; set; }
        
        [Required]
        public decimal Amount { get; set; }
        
        [Required]
        public string Currency { get; set; } = "BDT";
        
        [Required]
        public string PaymentMethod { get; set; } // "bKash", "Nagad", "Rocket", "Bank"
        
        [Required]
        public string TransactionId { get; set; } // External payment gateway transaction ID
        
        [Required]
        public string Status { get; set; } // "Pending", "Completed", "Failed", "Refunded"
        
        public string? GatewayResponse { get; set; } // JSON response from payment gateway
        
        public decimal? AdminCommission { get; set; }
        public decimal? ProviderAmount { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? CompletedAt { get; set; }
        
        public string? FailureReason { get; set; }
        public string? RefundReason { get; set; }
        public DateTime? RefundedAt { get; set; }
    }
}

