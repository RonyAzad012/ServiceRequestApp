namespace ServiceRequestApp.Models
{
    public class AcceptedRequest
    {
        public int Id { get; set; }
        public int ServiceRequestId { get; set; }
        public string ProviderId { get; set; }
        public DateTime AcceptedAt { get; set; }
        public string Status { get; set; } // InProgress, Completed
        public virtual ServiceRequest? ServiceRequest { get; set; }
        public virtual ApplicationUser? Provider { get; set; }
    }
}
