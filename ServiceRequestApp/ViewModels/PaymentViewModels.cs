using System.ComponentModel.DataAnnotations;

namespace ServiceRequestApp.ViewModels
{
    public class PaymentProcessViewModel
    {
        public int ServiceRequestId { get; set; }
        
        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
        public decimal Amount { get; set; }
        
        [Required]
        public string PaymentMethod { get; set; } = "Card";
        
        // Card details
        [StringLength(19)]
        public string? CardNumber { get; set; }
        
        [StringLength(5)]
        public string? ExpiryDate { get; set; }
        
        [StringLength(4)]
        public string? CVV { get; set; }
        
        [StringLength(100)]
        public string? CardholderName { get; set; }
        
        // Mobile banking details
        [StringLength(15)]
        public string? MobileNumber { get; set; }
        
        [StringLength(10)]
        public string? Pin { get; set; }
    }

    public class RefundRequestViewModel
    {
        [Required]
        public string TransactionId { get; set; }
        
        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
        public decimal Amount { get; set; }
        
        [Required]
        [StringLength(500)]
        public string Reason { get; set; }
    }
}
