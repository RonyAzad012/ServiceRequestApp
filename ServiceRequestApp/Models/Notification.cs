using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ServiceRequestApp.Models
{
    public class Notification
    {
        public int Id { get; set; }
        
        [Required]
        public string UserId { get; set; }
        
        [ForeignKey("UserId")]
        public virtual ApplicationUser User { get; set; }
        
        [Required]
        [StringLength(200)]
        public string Title { get; set; }
        
        [Required]
        [StringLength(1000)]
        public string Message { get; set; }
        
        [StringLength(50)]
        public string Type { get; set; } = "info"; // info, success, warning, error
        
        public bool IsRead { get; set; } = false;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime? ReadAt { get; set; }
        
        [StringLength(500)]
        public string? ActionUrl { get; set; }
        
        [StringLength(100)]
        public string? Icon { get; set; }
    }
}
