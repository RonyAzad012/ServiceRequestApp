using Microsoft.EntityFrameworkCore;
using ServiceRequestApp.Data;
using ServiceRequestApp.Models;

namespace ServiceRequestApp.Services
{
    public interface IInvoiceService
    {
        Task<InvoiceResult> GenerateInvoiceAsync(int serviceRequestId, string paymentTransactionId);
        Task<InvoiceResult> GetInvoiceAsync(int invoiceId);
        Task<InvoiceResult> GetInvoicesForUserAsync(string userId);
        Task<InvoiceResult> GetInvoicesForServiceRequestAsync(int serviceRequestId);
    }

    public class InvoiceService : IInvoiceService
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IConfiguration _configuration;

        public InvoiceService(ApplicationDbContext dbContext, IConfiguration configuration)
        {
            _dbContext = dbContext;
            _configuration = configuration;
        }

        public async Task<InvoiceResult> GenerateInvoiceAsync(int serviceRequestId, string paymentTransactionId)
        {
            try
            {
                var serviceRequest = await _dbContext.ServiceRequests
                    .Include(sr => sr.Requester)
                    .Include(sr => sr.Provider)
                    .Include(sr => sr.AcceptedRequest)
                    .ThenInclude(ar => ar.Provider)
                    .FirstOrDefaultAsync(sr => sr.Id == serviceRequestId);

                if (serviceRequest == null)
                {
                    return new InvoiceResult
                    {
                        Success = false,
                        Message = "Service request not found"
                    };
                }

                // Check if invoice already exists
                var existingInvoice = await _dbContext.Invoices
                    .FirstOrDefaultAsync(i => i.ServiceRequestId == serviceRequestId);

                if (existingInvoice != null)
                {
                    return new InvoiceResult
                    {
                        Success = true,
                        Message = "Invoice already exists",
                        Invoice = existingInvoice
                    };
                }

                // Get the provider from accepted request or direct assignment
                var provider = serviceRequest.AcceptedRequest?.Provider ?? serviceRequest.Provider;
                if (provider == null)
                {
                    return new InvoiceResult
                    {
                        Success = false,
                        Message = "No provider assigned to this service request"
                    };
                }

                // Generate invoice number
                var invoiceNumber = GenerateInvoiceNumber();

                // Calculate amounts
                var totalAmount = serviceRequest.PaymentAmount ?? serviceRequest.Budget ?? 0;
                var adminCommission = totalAmount * 0.05m; // 5% admin commission
                var providerAmount = totalAmount - adminCommission;

                var invoice = new Invoice
                {
                    ServiceRequestId = serviceRequestId,
                    RequesterId = serviceRequest.RequesterId,
                    ProviderId = provider.Id,
                    InvoiceNumber = invoiceNumber,
                    TotalAmount = totalAmount,
                    AdminCommission = adminCommission,
                    ProviderAmount = providerAmount,
                    Currency = "BDT",
                    Status = "Paid", // Since payment is already completed
                    CreatedAt = DateTime.UtcNow,
                    PaidAt = DateTime.UtcNow,
                    PaymentTransactionId = paymentTransactionId,
                    ServiceDescription = serviceRequest.Description,
                    ServiceLocation = serviceRequest.Address,
                    ServiceDate = serviceRequest.CreatedAt,
                    PaymentMethod = "SSLCommerz",
                    Notes = $"Payment completed for service request #{serviceRequestId}"
                };

                _dbContext.Invoices.Add(invoice);
                await _dbContext.SaveChangesAsync();

                return new InvoiceResult
                {
                    Success = true,
                    Message = "Invoice generated successfully",
                    Invoice = invoice
                };
            }
            catch (Exception ex)
            {
                return new InvoiceResult
                {
                    Success = false,
                    Message = $"Invoice generation failed: {ex.Message}"
                };
            }
        }

        public async Task<InvoiceResult> GetInvoiceAsync(int invoiceId)
        {
            try
            {
                var invoice = await _dbContext.Invoices
                    .Include(i => i.ServiceRequest)
                    .Include(i => i.Requester)
                    .Include(i => i.Provider)
                    .FirstOrDefaultAsync(i => i.Id == invoiceId);

                if (invoice == null)
                {
                    return new InvoiceResult
                    {
                        Success = false,
                        Message = "Invoice not found"
                    };
                }

                return new InvoiceResult
                {
                    Success = true,
                    Invoice = invoice
                };
            }
            catch (Exception ex)
            {
                return new InvoiceResult
                {
                    Success = false,
                    Message = $"Failed to retrieve invoice: {ex.Message}"
                };
            }
        }

        public async Task<InvoiceResult> GetInvoicesForUserAsync(string userId)
        {
            try
            {
                var invoices = await _dbContext.Invoices
                    .Include(i => i.ServiceRequest)
                    .Include(i => i.Requester)
                    .Include(i => i.Provider)
                    .Where(i => i.RequesterId == userId || i.ProviderId == userId)
                    .OrderByDescending(i => i.CreatedAt)
                    .ToListAsync();

                return new InvoiceResult
                {
                    Success = true,
                    Invoices = invoices
                };
            }
            catch (Exception ex)
            {
                return new InvoiceResult
                {
                    Success = false,
                    Message = $"Failed to retrieve invoices: {ex.Message}"
                };
            }
        }

        public async Task<InvoiceResult> GetInvoicesForServiceRequestAsync(int serviceRequestId)
        {
            try
            {
                var invoices = await _dbContext.Invoices
                    .Include(i => i.ServiceRequest)
                    .Include(i => i.Requester)
                    .Include(i => i.Provider)
                    .Where(i => i.ServiceRequestId == serviceRequestId)
                    .ToListAsync();

                return new InvoiceResult
                {
                    Success = true,
                    Invoices = invoices
                };
            }
            catch (Exception ex)
            {
                return new InvoiceResult
                {
                    Success = false,
                    Message = $"Failed to retrieve invoices: {ex.Message}"
                };
            }
        }

        private string GenerateInvoiceNumber()
        {
            var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            var random = new Random().Next(1000, 9999);
            return $"INV-{timestamp}-{random}";
        }
    }

    public class InvoiceResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public Invoice? Invoice { get; set; }
        public IEnumerable<Invoice>? Invoices { get; set; }
    }
}
