using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations.Schema;
namespace ServiceRequestApp.Models
{
    public class ServiceRequest
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string ServiceType { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Status { get; set; } // Pending, Accepted, Completed
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string RequesterId { get; set; }
        public virtual ApplicationUser? Requester { get; set; }
        public virtual AcceptedRequest? AcceptedRequest { get; set; }
        // New fields
        public string Address { get; set; }
        public string Zipcode { get; set; }
        public decimal Price { get; set; }
        public string PhoneNumber { get; set; }
        public DateTime? Deadline { get; set; }
        // Store image paths as comma-separated string or use a related table for multiple images
        public string? ImagePaths { get; set; }
        public string? PaymentStatus { get; set; } // Pending, Paid, Failed, Refunded
        public string? PaymentTransactionId { get; set; }
        public decimal? PaymentAmount { get; set; }
        public DateTime? PaymentDate { get; set; }
        public decimal? AdminCommission { get; set; } // Admin's percentage
        public decimal? ProviderAmount { get; set; } // Amount provider receives
        public int? CategoryId { get; set; }
        public virtual Category? Category { get; set; }
        
        // Completion tracking
        public DateTime? CompletedAt { get; set; }
        public DateTime? CancelledAt { get; set; }
        public string? CancellationReason { get; set; }
        public string? CompletionRejectionReason { get; set; }
        public DateTime? UpdatedAt { get; set; } = DateTime.UtcNow;
        
        public virtual ICollection<Message> Messages { get; set; } = new List<Message>();
    }
}
