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
        public virtual ICollection<ServiceRequest>? Requests { get; set; }
        public virtual ICollection<AcceptedRequest>? AcceptedRequests { get; set; }
    }
}
