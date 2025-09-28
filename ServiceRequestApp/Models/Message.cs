using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ServiceRequestApp.Models
{
    public class Message
    {
        public int Id { get; set; }
        [Required]
        public int ServiceRequestId { get; set; }
        [ForeignKey("ServiceRequestId")]
        public ServiceRequest ServiceRequest { get; set; }
        [Required]
        public string SenderId { get; set; }
        [ForeignKey("SenderId")]
        public ApplicationUser Sender { get; set; }
        [Required]
        public string ReceiverId { get; set; }
        [ForeignKey("ReceiverId")]
        public ApplicationUser Receiver { get; set; }
        [Required]
        public string Content { get; set; }
        public DateTime SentAt { get; set; } = DateTime.UtcNow;
        public bool IsRead { get; set; } = false;
        public DateTime? ReadAt { get; set; }
    }
}
