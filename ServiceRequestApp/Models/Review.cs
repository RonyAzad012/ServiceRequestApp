using System.ComponentModel.DataAnnotations;
using System;

namespace ServiceRequestApp.Models
{
    public class Review
    {
        public int Id { get; set; }
        public int ServiceRequestId { get; set; }
        public string ReviewerId { get; set; } // Requester
        public string RevieweeId { get; set; } // Provider
        [Range(1,5)]
        public int Rating { get; set; }
        [StringLength(500)]
        public string? Comment { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public virtual ServiceRequest? ServiceRequest { get; set; }
        public virtual ApplicationUser? Reviewer { get; set; }
        public virtual ApplicationUser? Reviewee { get; set; }
    }
}
