using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations.Schema;
namespace ServiceRequestApp.Models
{
    public class ServiceRequest
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(200)]
        public string Title { get; set; }
        
        [Required]
        [StringLength(2000)]
        public string Description { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "Pending"; // Pending, In Progress, Completed, Cancelled
        
        // Location
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        
        [Required]
        [StringLength(500)]
        public string Address { get; set; }
        
        [Required]
        [StringLength(20)]
        public string Zipcode { get; set; }
        
        // User relationships
        [Required]
        public string RequesterId { get; set; }
        public virtual ApplicationUser? Requester { get; set; }
        
        public string? ProviderId { get; set; }
        public virtual ApplicationUser? Provider { get; set; }
        
        // Category relationship
        [Required]
        public int CategoryId { get; set; }
        public virtual Category? Category { get; set; }
        
        // Pricing and budget
        public decimal? Budget { get; set; } // Estimated budget range
        public string? BudgetType { get; set; } // Fixed, Hourly, Negotiable
        
        // Timeline
        public DateTime? PreferredDate { get; set; }
        public DateTime? Deadline { get; set; }
        public string? Urgency { get; set; } // Low, Medium, High, Urgent
        
        // Contact
        [Required]
        [StringLength(20)]
        public string PhoneNumber { get; set; }
        
        // Additional details
        public string? SpecialRequirements { get; set; }
        public string? AttachedFiles { get; set; } // JSON string for multiple file paths
        
        // Payment tracking
        public string? PaymentStatus { get; set; } = "Pending"; // Pending, Paid, Failed, Refunded
        public string? PaymentTransactionId { get; set; }
        public decimal? PaymentAmount { get; set; }
        public DateTime? PaymentDate { get; set; }
        public decimal? AdminCommission { get; set; }
        public decimal? ProviderAmount { get; set; }
        
        // Completion tracking
        public DateTime? CompletedAt { get; set; }
        public DateTime? CancelledAt { get; set; }
        public string? CancellationReason { get; set; }
        public string? CompletionRejectionReason { get; set; }
        public DateTime? UpdatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation properties
        public virtual AcceptedRequest? AcceptedRequest { get; set; }
        public virtual ICollection<Message> Messages { get; set; } = new List<Message>();
    }
}
