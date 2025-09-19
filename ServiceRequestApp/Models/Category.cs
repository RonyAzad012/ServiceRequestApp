using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ServiceRequestApp.Models
{
    public class Category
    {
        public int Id { get; set; }
        [Required]
        public string Name { get; set; }
        public ICollection<ServiceRequest> ServiceRequests { get; set; }
    }
}
