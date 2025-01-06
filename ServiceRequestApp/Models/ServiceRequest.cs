using System.ComponentModel.DataAnnotations;

namespace ServiceRequestApp.Models
{
    public class ServiceRequest
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Title { get; set; }

        [Required]
        public string Description { get; set; }

        [Required]
        public string RequestedBy { get; set; }

        [Required]
        [DataType(DataType.DateTime)]
        public DateTime RequestedAt { get; set; }

        public bool IsCompleted { get; set; } = false;
    }
}
