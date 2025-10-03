using Microsoft.EntityFrameworkCore;
using ServiceRequestApp.Data;
using ServiceRequestApp.Models;

namespace ServiceRequestApp.Services
{
    public interface IPaymentCompletionService
    {
        Task<PaymentCompletionResult> CompletePaymentAsync(string transactionId, string valId);
        Task<PaymentCompletionResult> ProcessMoneyFlowAsync(int serviceRequestId, decimal totalAmount);
        Task<decimal> CalculateAdminCommissionAsync(decimal amount);
        Task<decimal> CalculateProviderAmountAsync(decimal amount, decimal adminCommission);
    }

    public class PaymentCompletionService : IPaymentCompletionService
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IConfiguration _configuration;

        public PaymentCompletionService(ApplicationDbContext dbContext, IConfiguration configuration)
        {
            _dbContext = dbContext;
            _configuration = configuration;
        }

        public async Task<PaymentCompletionResult> CompletePaymentAsync(string transactionId, string valId)
        {
            try
            {
                // Find the transaction
                var transaction = await _dbContext.PaymentTransactions
                    .Include(pt => pt.ServiceRequest)
                    .ThenInclude(sr => sr.AcceptedRequest)
                    .ThenInclude(ar => ar.Provider)
                    .FirstOrDefaultAsync(pt => pt.TransactionId == transactionId);

                if (transaction == null)
                {
                    return new PaymentCompletionResult
                    {
                        Success = false,
                        Message = "Transaction not found"
                    };
                }

                // Check if already completed
                if (transaction.Status == "Completed")
                {
                    return new PaymentCompletionResult
                    {
                        Success = true,
                        Message = "Payment already completed",
                        TransactionId = transactionId,
                        ServiceRequestId = transaction.ServiceRequestId,
                        AdminCommission = transaction.AdminCommissionAmount ?? 0,
                        ProviderAmount = transaction.ProviderReceivedAmount ?? 0,
                        TotalAmount = transaction.Amount
                    };
                }

                // Update transaction status
                transaction.Status = "Completed";
                transaction.CompletedAt = DateTime.UtcNow;
                transaction.GatewayResponse = valId; // Store valId as part of gateway response

                // Process money flow
                var moneyFlowResult = await ProcessMoneyFlowAsync(transaction.ServiceRequestId, transaction.Amount);
                if (!moneyFlowResult.Success)
                {
                    return moneyFlowResult;
                }

                // Update transaction with calculated amounts
                transaction.AdminCommissionAmount = moneyFlowResult.AdminCommission;
                transaction.ProviderReceivedAmount = moneyFlowResult.ProviderAmount;

                // Update service request payment status and mark as completed
                var serviceRequest = transaction.ServiceRequest;
                serviceRequest.PaymentStatus = "Paid";
                serviceRequest.PaymentTransactionId = transactionId;
                serviceRequest.PaymentAmount = transaction.Amount;
                serviceRequest.PaymentDate = DateTime.UtcNow;
                serviceRequest.AdminCommission = moneyFlowResult.AdminCommission;
                serviceRequest.ProviderAmount = moneyFlowResult.ProviderAmount;
                
                // Mark service request as completed since payment is successful
                serviceRequest.Status = "Completed";
                serviceRequest.CompletedAt = DateTime.UtcNow;

                await _dbContext.SaveChangesAsync();

                return new PaymentCompletionResult
                {
                    Success = true,
                    Message = "Payment completed successfully",
                    TransactionId = transactionId,
                    ServiceRequestId = serviceRequest.Id,
                    AdminCommission = moneyFlowResult.AdminCommission,
                    ProviderAmount = moneyFlowResult.ProviderAmount,
                    TotalAmount = transaction.Amount
                };
            }
            catch (Exception ex)
            {
                return new PaymentCompletionResult
                {
                    Success = false,
                    Message = $"Payment completion failed: {ex.Message}"
                };
            }
        }

        public async Task<PaymentCompletionResult> ProcessMoneyFlowAsync(int serviceRequestId, decimal totalAmount)
        {
            try
            {
                var serviceRequest = await _dbContext.ServiceRequests
                    .Include(sr => sr.AcceptedRequest)
                    .ThenInclude(ar => ar.Provider)
                    .FirstOrDefaultAsync(sr => sr.Id == serviceRequestId);

                if (serviceRequest == null)
                {
                    return new PaymentCompletionResult
                    {
                        Success = false,
                        Message = "Service request not found"
                    };
                }

                // Calculate commission and provider amount regardless of provider assignment
                // This allows payment completion even if no provider is assigned yet
                var adminCommission = await CalculateAdminCommissionAsync(totalAmount);
                var providerAmount = await CalculateProviderAmountAsync(totalAmount, adminCommission);

                return new PaymentCompletionResult
                {
                    Success = true,
                    Message = "Money flow processed successfully",
                    AdminCommission = adminCommission,
                    ProviderAmount = providerAmount,
                    TotalAmount = totalAmount
                };
            }
            catch (Exception ex)
            {
                return new PaymentCompletionResult
                {
                    Success = false,
                    Message = $"Money flow processing failed: {ex.Message}"
                };
            }
        }

        public async Task<decimal> CalculateAdminCommissionAsync(decimal amount)
        {
            // Get commission rate from configuration (default 5%)
            var commissionRate = _configuration.GetValue<decimal>("PaymentSettings:AdminCommissionRate", 0.05m);
            return Math.Round(amount * commissionRate, 2);
        }

        public async Task<decimal> CalculateProviderAmountAsync(decimal amount, decimal adminCommission)
        {
            return Math.Round(amount - adminCommission, 2);
        }
    }

    public class PaymentCompletionResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? TransactionId { get; set; }
        public int? ServiceRequestId { get; set; }
        public decimal AdminCommission { get; set; }
        public decimal ProviderAmount { get; set; }
        public decimal TotalAmount { get; set; }
    }
}


