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
    }
}