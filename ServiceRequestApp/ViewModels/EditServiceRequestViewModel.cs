using System.ComponentModel.DataAnnotations;

namespace ServiceRequestApp.ViewModels
{
    public class EditServiceRequestViewModel
    {
        [Required]
        [StringLength(100)]
        public string Title { get; set; }

        [Required]
        public string Description { get; set; }
    }
}