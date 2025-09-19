using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace ServiceRequestApp.ViewModels
{
    public class EditServiceRequestViewModel
    {
        [Required]
        [StringLength(100)]
        public string Title { get; set; }

        [Required]
        public string Description { get; set; }

        // Category selection
        [Display(Name = "Category")]
        public int? CategoryId { get; set; }
        public IEnumerable<ServiceRequestApp.Models.Category>? Categories { get; set; }
    }
}