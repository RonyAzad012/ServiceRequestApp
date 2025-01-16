using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
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

    }
}
