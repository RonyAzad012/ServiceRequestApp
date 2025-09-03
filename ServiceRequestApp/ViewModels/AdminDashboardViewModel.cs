namespace ServiceRequestApp.ViewModels
{
    public class AdminDashboardViewModel
    {
        public int UserCount { get; set; }
        public int ServiceRequestCount { get; set; }
        public int CompletedRequestCount { get; set; }
        public int PaidRequestCount { get; set; }
        public decimal TotalPayments { get; set; }
        public int ProviderCount { get; set; }
        public int RequesterCount { get; set; }
    }
}
