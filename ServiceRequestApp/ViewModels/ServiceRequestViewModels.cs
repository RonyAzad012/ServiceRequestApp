using System.ComponentModel.DataAnnotations;

namespace ServiceRequestApp.ViewModels
{
    public class CreateServiceRequestViewModel
    {
        [Required]
        public string Title { get; set; }

        [Required]
        public string Description { get; set; }

        [Required]
        public string ServiceType { get; set; }

        [Required]
        public double Latitude { get; set; }

        [Required]
        public double Longitude { get; set; }

        [Required]
        public string Address { get; set; }

        [Required]
        public string Zipcode { get; set; }

        [Required]
        public decimal Price { get; set; }

        [Required]
        public string PhoneNumber { get; set; }

        public DateTime? Deadline { get; set; }

        public List<IFormFile>? Images { get; set; }
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