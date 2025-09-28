using ServiceRequestApp.Models;
using ServiceRequestApp.Data;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text;

namespace ServiceRequestApp.Services
{
    public interface IPaymentService
    {
        Task<PaymentResult> ProcessPaymentAsync(PaymentRequest request);
        Task<PaymentResult> VerifyPaymentAsync(string transactionId);
        Task<PaymentResult> RefundPaymentAsync(string transactionId, decimal amount);
        Task<decimal> CalculateAdminCommissionAsync(decimal amount);
        Task<decimal> CalculateProviderAmountAsync(decimal amount);
    }

    public class PaymentService : IPaymentService
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;

        public PaymentService(ApplicationDbContext dbContext, IConfiguration configuration, HttpClient httpClient)
        {
            _dbContext = dbContext;
            _configuration = configuration;
            _httpClient = httpClient;
        }

        public async Task<PaymentResult> ProcessPaymentAsync(PaymentRequest request)
        {
            try
            {
                // Calculate amounts
                var adminCommission = await CalculateAdminCommissionAsync(request.Amount);
                var providerAmount = await CalculateProviderAmountAsync(request.Amount);

                // For demo purposes, we'll simulate a payment gateway
                // In production, integrate with actual payment gateway like SSL Commerz, bKash, etc.
                var transactionId = GenerateTransactionId();
                
                // Simulate payment processing
                var paymentResult = await SimulatePaymentProcessing(request, transactionId);

                if (paymentResult.Success)
                {
                    // Create payment transaction record
                    var paymentTransaction = new PaymentTransaction
                    {
                        UserId = request.UserId,
                        ServiceRequestId = request.ServiceRequestId,
                        Amount = request.Amount,
                        Currency = "BDT",
                        TransactionDate = DateTime.UtcNow,
                        Status = "Completed",
                        TransactionId = transactionId,
                        PaymentMethod = request.PaymentMethod,
                        AdminCommissionAmount = adminCommission,
                        ProviderReceivedAmount = providerAmount,
                        Remarks = $"Payment for service request #{request.ServiceRequestId}"
                    };

                    _dbContext.PaymentTransactions.Add(paymentTransaction);

                    // Update service request with payment information
                    var serviceRequest = await _dbContext.ServiceRequests
                        .FirstOrDefaultAsync(sr => sr.Id == request.ServiceRequestId);
                    
                    if (serviceRequest != null)
                    {
                        serviceRequest.PaymentStatus = "Paid";
                        serviceRequest.PaymentTransactionId = transactionId;
                        serviceRequest.PaymentAmount = request.Amount;
                        serviceRequest.PaymentDate = DateTime.UtcNow;
                        serviceRequest.AdminCommission = adminCommission;
                        serviceRequest.ProviderAmount = providerAmount;
                    }

                    await _dbContext.SaveChangesAsync();

                    return new PaymentResult
                    {
                        Success = true,
                        TransactionId = transactionId,
                        Amount = request.Amount,
                        AdminCommission = adminCommission,
                        ProviderAmount = providerAmount,
                        Message = "Payment processed successfully"
                    };
                }
                else
                {
                    return new PaymentResult
                    {
                        Success = false,
                        Message = paymentResult.Message
                    };
                }
            }
            catch (Exception ex)
            {
                return new PaymentResult
                {
                    Success = false,
                    Message = $"Payment processing failed: {ex.Message}"
                };
            }
        }

        public async Task<PaymentResult> VerifyPaymentAsync(string transactionId)
        {
            try
            {
                var transaction = await _dbContext.PaymentTransactions
                    .FirstOrDefaultAsync(pt => pt.TransactionId == transactionId);

                if (transaction == null)
                {
                    return new PaymentResult
                    {
                        Success = false,
                        Message = "Transaction not found"
                    };
                }

                // In production, verify with actual payment gateway
                var verificationResult = await SimulatePaymentVerification(transactionId);

                if (verificationResult.Success)
                {
                    transaction.Status = "Verified";
                    await _dbContext.SaveChangesAsync();

                    return new PaymentResult
                    {
                        Success = true,
                        TransactionId = transactionId,
                        Amount = transaction.Amount,
                        Message = "Payment verified successfully"
                    };
                }
                else
                {
                    return new PaymentResult
                    {
                        Success = false,
                        Message = "Payment verification failed"
                    };
                }
            }
            catch (Exception ex)
            {
                return new PaymentResult
                {
                    Success = false,
                    Message = $"Verification failed: {ex.Message}"
                };
            }
        }

        public async Task<PaymentResult> RefundPaymentAsync(string transactionId, decimal amount)
        {
            try
            {
                var transaction = await _dbContext.PaymentTransactions
                    .FirstOrDefaultAsync(pt => pt.TransactionId == transactionId);

                if (transaction == null)
                {
                    return new PaymentResult
                    {
                        Success = false,
                        Message = "Transaction not found"
                    };
                }

                if (amount > transaction.Amount)
                {
                    return new PaymentResult
                    {
                        Success = false,
                        Message = "Refund amount cannot exceed original payment"
                    };
                }

                // Simulate refund processing
                var refundResult = await SimulateRefundProcessing(transactionId, amount);

                if (refundResult.Success)
                {
                    // Create refund transaction record
                    var refundTransaction = new PaymentTransaction
                    {
                        UserId = transaction.UserId,
                        ServiceRequestId = transaction.ServiceRequestId,
                        Amount = -amount, // Negative amount for refund
                        Currency = "BDT",
                        TransactionDate = DateTime.UtcNow,
                        Status = "Refunded",
                        TransactionId = GenerateTransactionId(),
                        PaymentMethod = transaction.PaymentMethod,
                        Remarks = $"Refund for transaction {transactionId}"
                    };

                    _dbContext.PaymentTransactions.Add(refundTransaction);
                    await _dbContext.SaveChangesAsync();

                    return new PaymentResult
                    {
                        Success = true,
                        TransactionId = refundTransaction.TransactionId,
                        Amount = amount,
                        Message = "Refund processed successfully"
                    };
                }
                else
                {
                    return new PaymentResult
                    {
                        Success = false,
                        Message = "Refund processing failed"
                    };
                }
            }
            catch (Exception ex)
            {
                return new PaymentResult
                {
                    Success = false,
                    Message = $"Refund failed: {ex.Message}"
                };
            }
        }

        public async Task<decimal> CalculateAdminCommissionAsync(decimal amount)
        {
            // Get admin commission rate from configuration (default 5%)
            var commissionRate = _configuration.GetValue<decimal>("PaymentSettings:AdminCommissionRate", 0.05m);
            return Math.Round(amount * commissionRate, 2);
        }

        public async Task<decimal> CalculateProviderAmountAsync(decimal amount)
        {
            var adminCommission = await CalculateAdminCommissionAsync(amount);
            return Math.Round(amount - adminCommission, 2);
        }

        private string GenerateTransactionId()
        {
            return $"TXN_{DateTime.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid().ToString("N")[..8].ToUpper()}";
        }

        private async Task<PaymentResult> SimulatePaymentProcessing(PaymentRequest request, string transactionId)
        {
            // Simulate API call delay
            await Task.Delay(1000);

            // Simulate payment gateway response
            // In production, this would be actual API call to payment gateway
            var random = new Random();
            var success = random.NextDouble() > 0.1; // 90% success rate for demo

            return new PaymentResult
            {
                Success = success,
                TransactionId = transactionId,
                Message = success ? "Payment processed successfully" : "Payment failed - insufficient funds"
            };
        }

        private async Task<PaymentResult> SimulatePaymentVerification(string transactionId)
        {
            await Task.Delay(500);
            return new PaymentResult
            {
                Success = true,
                TransactionId = transactionId,
                Message = "Payment verified"
            };
        }

        private async Task<PaymentResult> SimulateRefundProcessing(string transactionId, decimal amount)
        {
            await Task.Delay(1000);
            return new PaymentResult
            {
                Success = true,
                TransactionId = transactionId,
                Message = "Refund processed"
            };
        }
    }

    // Request and Result models
    public class PaymentRequest
    {
        public string UserId { get; set; }
        public int ServiceRequestId { get; set; }
        public decimal Amount { get; set; }
        public string PaymentMethod { get; set; } = "Card";
        public string Currency { get; set; } = "BDT";
        public string CustomerName { get; set; }
        public string CustomerEmail { get; set; }
        public string CustomerPhone { get; set; }
        public string CustomerAddress { get; set; }
        public Dictionary<string, string> AdditionalData { get; set; } = new();
    }

    public class PaymentResult
    {
        public bool Success { get; set; }
        public string TransactionId { get; set; }
        public decimal Amount { get; set; }
        public decimal AdminCommission { get; set; }
        public decimal ProviderAmount { get; set; }
        public string Message { get; set; }
        public Dictionary<string, object> AdditionalData { get; set; } = new();
    }
}
