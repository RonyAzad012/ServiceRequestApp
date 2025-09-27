using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
namespace ServiceRequestApp.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Address { get; set; }
        public string UserType { get; set; } // "Provider" or "Requester"
        public string? Zipcode { get; set; }
        public string? NationalId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string? BusinessCredentials { get; set; } // For providers
        public string? BusinessImagePath { get; set; } // For providers
        public string? ShopName { get; set; } // For providers
        public string? ShopDescription { get; set; } // For providers
        public string? ShopPhone { get; set; } // For providers
        public string? ShopAddress { get; set; } // For providers

        // Tasker-specific fields
        public string? Skills { get; set; } // Comma-separated skills
        public string? PortfolioUrl { get; set; }
        public string? ProfileDescription { get; set; }

        public virtual ICollection<ServiceRequest>? Requests { get; set; }
        public virtual ICollection<ServiceRequest>? ServiceRequests { get; set; }
        public virtual ICollection<AcceptedRequest>? AcceptedRequests { get; set; }
    }
}
