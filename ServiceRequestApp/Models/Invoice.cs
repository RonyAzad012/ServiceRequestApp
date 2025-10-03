using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ServiceRequestApp.Models
{
    public class Invoice
    {
        public int Id { get; set; }
        
        [Required]
        public int ServiceRequestId { get; set; }
        public virtual ServiceRequest ServiceRequest { get; set; }
        
        [Required]
        public string RequesterId { get; set; }
        public virtual ApplicationUser Requester { get; set; }
        
        [Required]
        public string ProviderId { get; set; }
        public virtual ApplicationUser Provider { get; set; }
        
        [Required]
        public string InvoiceNumber { get; set; }
        
        [Required]
        public decimal TotalAmount { get; set; }
        
        [Required]
        public decimal AdminCommission { get; set; }
        
        [Required]
        public decimal ProviderAmount { get; set; }
        
        [Required]
        public string Currency { get; set; } = "BDT";
        
        [Required]
        public string Status { get; set; } = "Generated"; // Generated, Sent, Paid, Cancelled
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? SentAt { get; set; }
        public DateTime? PaidAt { get; set; }
        
        public string? PaymentTransactionId { get; set; }
        public string? Notes { get; set; }
        
        // Invoice details
        public string? ServiceDescription { get; set; }
        public string? ServiceLocation { get; set; }
        public DateTime? ServiceDate { get; set; }
        public string? PaymentMethod { get; set; }
    }
}
