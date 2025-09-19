using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ServiceRequestApp.Models
{
    public class ServiceRequestApplication
    {
        public int Id { get; set; }
        [Required]
        public int ServiceRequestId { get; set; }
        [ForeignKey("ServiceRequestId")]
        public ServiceRequest ServiceRequest { get; set; }
        [Required]
        public string ProviderId { get; set; }
        [ForeignKey("ProviderId")]
        public ApplicationUser Provider { get; set; }
        public DateTime AppliedAt { get; set; } = DateTime.UtcNow;
        public string? Message { get; set; }
        public decimal? OfferedPrice { get; set; }
        [Required]
        public string Status { get; set; } = "Pending"; // Pending, Accepted, Rejected
    }
}
