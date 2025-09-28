using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ServiceRequestApp.Models
{
    public class Category
    {
        public int Id { get; set; }
        [Required]
        public string Name { get; set; }
        public string? Description { get; set; }
        public string? Icon { get; set; } // FontAwesome icon class
        public string? Color { get; set; } // Hex color code
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation properties
        public ICollection<ServiceRequest> ServiceRequests { get; set; } = new List<ServiceRequest>();
        public ICollection<ApplicationUser> Providers { get; set; } = new List<ApplicationUser>();
    }
}
