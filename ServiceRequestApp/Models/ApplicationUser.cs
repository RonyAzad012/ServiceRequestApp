using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;

namespace ServiceRequestApp.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Address { get; set; }
        public string UserType { get; set; } // "Provider", "Requester", "Tasker", "Business"
        public string? Zipcode { get; set; }
        public string? NationalId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // Approval system
        public bool IsApproved { get; set; } = false;
        public DateTime? ApprovedAt { get; set; }
        public string? ApprovedBy { get; set; }
        public string? RejectionReason { get; set; }
        
        // Business/Provider fields
        public string? BusinessCredentials { get; set; } // For providers
        public string? BusinessImagePath { get; set; } // For providers
        public string? ShopName { get; set; } // For providers
        public string? ShopDescription { get; set; } // For providers
        public string? ShopPhone { get; set; } // For providers
        public string? ShopAddress { get; set; } // For providers
        public string? BusinessLicense { get; set; }
        public string? TaxId { get; set; }
        public string? BusinessWebsite { get; set; } // Business website URL
        public string? BusinessDocuments { get; set; } // JSON string for multiple file paths
        
        // Tasker-specific fields
        public string? Skills { get; set; } // Comma-separated skills
        public string? PortfolioUrl { get; set; }
        public string? ProfileDescription { get; set; }
        public string? ProfileImagePath { get; set; }
        
        // Location for map display
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        
        // Rating and reviews
        public decimal? AverageRating { get; set; }
        public int? TotalReviews { get; set; }
        
        // Category relationship
        public int? PrimaryCategoryId { get; set; }
        public virtual Category? PrimaryCategory { get; set; }
        
        // Service areas (comma-separated)
        public string? ServiceAreas { get; set; }
        
        // Availability
        public bool IsAvailable { get; set; } = true;
        public string? AvailabilitySchedule { get; set; } // JSON string for schedule

        public virtual ICollection<ServiceRequest>? Requests { get; set; }
        public virtual ICollection<ServiceRequest>? ServiceRequests { get; set; }
        public virtual ICollection<AcceptedRequest>? AcceptedRequests { get; set; }
        public virtual ICollection<Review>? Reviews { get; set; }
    }
}
