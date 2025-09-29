using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;

namespace ServiceRequestApp.ViewModels
{
    public class CreateServiceRequestViewModel
    {
        [Required]
        [StringLength(200, ErrorMessage = "Title cannot exceed 200 characters")]
        public string Title { get; set; }

        [Required]
        [StringLength(2000, ErrorMessage = "Description cannot exceed 2000 characters")]
        public string Description { get; set; }

        [Required]
        public int CategoryId { get; set; }

        [Required]
        [StringLength(500, ErrorMessage = "Address cannot exceed 500 characters")]
        public string Address { get; set; }

        [Required]
        [StringLength(20, ErrorMessage = "Zipcode cannot exceed 20 characters")]
        public string Zipcode { get; set; }

        [Required]
        [StringLength(20, ErrorMessage = "Phone number cannot exceed 20 characters")]
        public string PhoneNumber { get; set; }

        // Budget and pricing
        public decimal? Budget { get; set; }
        
        [Required]
        public string BudgetType { get; set; } = "Negotiable"; // Fixed, Hourly, Negotiable

        // Timeline
        public DateTime? PreferredDate { get; set; }
        public DateTime? Deadline { get; set; }
        
        [Required]
        public string Urgency { get; set; } = "Medium"; // Low, Medium, High, Urgent

        // Additional details
        public string? SpecialRequirements { get; set; }

        // File uploads
        public IFormFileCollection? AttachedFiles { get; set; }

        // Category selection
        public IEnumerable<ServiceRequestApp.Models.Category>? Categories { get; set; }
    }

    public class ProviderDashboardViewModel
    {
        public int AcceptedCount { get; set; }
        public int CompletedCount { get; set; }
        public decimal TotalEarnings { get; set; }
    }

    public class RequesterDashboardViewModel
    {
        public int CreatedCount { get; set; }
        public int CompletedCount { get; set; }
        public decimal TotalSpent { get; set; }
    }
}