namespace ServiceRequestApp.Models
{
    public class Review
    {
        public int Id { get; set; }
        public int ServiceRequestId { get; set; }
        public string ReviewerId { get; set; }
        public string RevieweeId { get; set; }
        public int Rating { get; set; }
        public string? Comment { get; set; }
        public DateTime CreatedAt { get; set; }
        public virtual ServiceRequest? ServiceRequest { get; set; }
        public virtual ApplicationUser? Reviewer { get; set; }
        public virtual ApplicationUser? Reviewee { get; set; }
    }
}
