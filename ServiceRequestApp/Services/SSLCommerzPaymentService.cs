using ServiceRequestApp.Models;
using ServiceRequestApp.Data;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text;
using System.Security.Cryptography;

namespace ServiceRequestApp.Services
{
    public interface ISSLCommerzPaymentService
    {
        Task<PaymentResult> CreatePaymentSessionAsync(PaymentRequest request);
        Task<PaymentResult> VerifyPaymentAsync(string sessionKey, string status);
        Task<PaymentResult> RefundPaymentAsync(string transactionId, decimal amount);
    }

    public class SSLCommerzPaymentService : ISSLCommerzPaymentService
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;
        private readonly string _storeId;
        private readonly string _storePassword;
        private readonly string _baseUrl;
        private readonly bool _isTestMode;

        public SSLCommerzPaymentService(ApplicationDbContext dbContext, IConfiguration configuration, HttpClient httpClient)
        {
            _dbContext = dbContext;
            _configuration = configuration;
            _httpClient = httpClient;
            
            _storeId = _configuration["PaymentSettings:SSLCommerz:StoreId"];
            _storePassword = _configuration["PaymentSettings:SSLCommerz:StorePassword"];
            _isTestMode = _configuration.GetValue<bool>("PaymentSettings:SSLCommerz:IsTestMode");
            _baseUrl = _isTestMode 
                ? "https://sandbox.sslcommerz.com" 
                : "https://securepay.sslcommerz.com";
        }

        public async Task<PaymentResult> CreatePaymentSessionAsync(PaymentRequest request)
        {
            try
            {
                // Calculate amounts
                var adminCommission = await CalculateAdminCommissionAsync(request.Amount);
                var providerAmount = await CalculateProviderAmountAsync(request.Amount);

                // Generate unique transaction ID
                var transactionId = GenerateTransactionId();
                var sessionKey = GenerateSessionKey();

                // Create payment data for SSL Commerz
                var paymentData = new
                {
                    store_id = _storeId,
                    store_passwd = _storePassword,
                    total_amount = request.Amount,
                    currency = "BDT",
                    tran_id = transactionId,
                    success_url = $"{_configuration["AppSettings:BaseUrl"]}/Payment/Success?session={sessionKey}",
                    fail_url = $"{_configuration["AppSettings:BaseUrl"]}/Payment/Fail?session={sessionKey}",
                    cancel_url = $"{_configuration["AppSettings:BaseUrl"]}/Payment/Cancel?session={sessionKey}",
                    ipn_url = $"{_configuration["AppSettings:BaseUrl"]}/Payment/IPN",
                    multi_card_name = "mastercard,visacard,amexcard",
                    product_name = $"Service Request #{request.ServiceRequestId}",
                    product_category = "Service",
                    product_profile = "general",
                    cus_name = request.CustomerName,
                    cus_email = request.CustomerEmail,
                    cus_add1 = request.CustomerAddress,
                    cus_city = "Dhaka",
                    cus_postcode = "1000",
                    cus_country = "Bangladesh",
                    cus_phone = request.CustomerPhone,
                    ship_name = request.CustomerName,
                    ship_add1 = request.CustomerAddress,
                    ship_city = "Dhaka",
                    ship_postcode = "1000",
                    ship_country = "Bangladesh",
                    value_a = sessionKey, // Custom field to store session key
                    value_b = request.ServiceRequestId.ToString(),
                    value_c = request.UserId,
                    value_d = adminCommission.ToString(),
                    value_e = providerAmount.ToString()
                };

                // Create payment transaction record
                var paymentTransaction = new PaymentTransaction
                {
                    UserId = request.UserId,
                    ServiceRequestId = request.ServiceRequestId,
                    Amount = request.Amount,
                    Currency = "BDT",
                    TransactionDate = DateTime.UtcNow,
                    Status = "Pending",
                    TransactionId = transactionId,
                    PaymentMethod = request.PaymentMethod,
                    AdminCommissionAmount = adminCommission,
                    ProviderReceivedAmount = providerAmount,
                    Remarks = $"Payment session created for service request #{request.ServiceRequestId}"
                };

                _dbContext.PaymentTransactions.Add(paymentTransaction);
                await _dbContext.SaveChangesAsync();

                // Send request to SSL Commerz
                var json = JsonSerializer.Serialize(paymentData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{_baseUrl}/gwprocess/v4/api.php", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var sslResponse = JsonSerializer.Deserialize<SSLCommerzResponse>(responseContent);
                    
                    if (sslResponse?.Status == "SUCCESS")
                    {
                        return new PaymentResult
                        {
                            Success = true,
                            TransactionId = transactionId,
                            Amount = request.Amount,
                            AdminCommission = adminCommission,
                            ProviderAmount = providerAmount,
                            Message = "Payment session created successfully",
                            AdditionalData = new Dictionary<string, object>
                            {
                                ["GatewayURL"] = sslResponse.GatewayPageURL,
                                ["SessionKey"] = sessionKey
                            }
                        };
                    }
                }

                return new PaymentResult
                {
                    Success = false,
                    Message = "Failed to create payment session"
                };
            }
            catch (Exception ex)
            {
                return new PaymentResult
                {
                    Success = false,
                    Message = $"Payment session creation failed: {ex.Message}"
                };
            }
        }

        public async Task<PaymentResult> VerifyPaymentAsync(string sessionKey, string status)
        {
            try
            {
                var transaction = await _dbContext.PaymentTransactions
                    .FirstOrDefaultAsync(pt => pt.Remarks.Contains(sessionKey));

                if (transaction == null)
                {
                    return new PaymentResult
                    {
                        Success = false,
                        Message = "Transaction not found"
                    };
                }

                // Verify with SSL Commerz
                var verificationData = new
                {
                    store_id = _storeId,
                    store_passwd = _storePassword,
                    val_id = sessionKey,
                    format = "json"
                };

                var json = JsonSerializer.Serialize(verificationData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{_baseUrl}/validator/api/validationserverAPI.php", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var verificationResponse = JsonSerializer.Deserialize<SSLCommerzVerificationResponse>(responseContent);
                    
                    if (verificationResponse?.Status == "VALID" && status == "VALID")
                    {
                        // Update transaction status
                        transaction.Status = "Completed";
                        transaction.TransactionDate = DateTime.UtcNow;

                        // Update service request
                        var serviceRequest = await _dbContext.ServiceRequests
                            .FirstOrDefaultAsync(sr => sr.Id == transaction.ServiceRequestId);
                        
                        if (serviceRequest != null)
                        {
                            serviceRequest.PaymentStatus = "Paid";
                            serviceRequest.PaymentTransactionId = transaction.TransactionId;
                            serviceRequest.PaymentAmount = transaction.Amount;
                            serviceRequest.PaymentDate = DateTime.UtcNow;
                            serviceRequest.AdminCommission = transaction.AdminCommissionAmount;
                            serviceRequest.ProviderAmount = transaction.ProviderReceivedAmount;
                        }

                        await _dbContext.SaveChangesAsync();

                        return new PaymentResult
                        {
                            Success = true,
                            TransactionId = transaction.TransactionId,
                            Amount = transaction.Amount,
                            AdminCommission = transaction.AdminCommissionAmount ?? 0,
                            ProviderAmount = transaction.ProviderReceivedAmount ?? 0,
                            Message = "Payment verified successfully"
                        };
                    }
                }

                // Mark as failed
                transaction.Status = "Failed";
                await _dbContext.SaveChangesAsync();

                return new PaymentResult
                {
                    Success = false,
                    Message = "Payment verification failed"
                };
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

                // SSL Commerz refund API call
                var refundData = new
                {
                    store_id = _storeId,
                    store_passwd = _storePassword,
                    refund_amount = amount,
                    refund_remarks = "Service request refund",
                    bank_tran_id = transactionId,
                    refe_id = GenerateTransactionId()
                };

                var json = JsonSerializer.Serialize(refundData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{_baseUrl}/api/refund", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var refundResponse = JsonSerializer.Deserialize<SSLCommerzRefundResponse>(responseContent);
                    
                    if (refundResponse?.Status == "SUCCESS")
                    {
                        // Create refund transaction record
                        var refundTransaction = new PaymentTransaction
                        {
                            UserId = transaction.UserId,
                            ServiceRequestId = transaction.ServiceRequestId,
                            Amount = -amount,
                            Currency = "BDT",
                            TransactionDate = DateTime.UtcNow,
                            Status = "Refunded",
                            TransactionId = refundResponse.RefundRefId,
                            PaymentMethod = transaction.PaymentMethod,
                            Remarks = $"Refund for transaction {transactionId}"
                        };

                        _dbContext.PaymentTransactions.Add(refundTransaction);
                        await _dbContext.SaveChangesAsync();

                        return new PaymentResult
                        {
                            Success = true,
                            TransactionId = refundResponse.RefundRefId,
                            Amount = amount,
                            Message = "Refund processed successfully"
                        };
                    }
                }

                return new PaymentResult
                {
                    Success = false,
                    Message = "Refund processing failed"
                };
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

        private async Task<decimal> CalculateAdminCommissionAsync(decimal amount)
        {
            var commissionRate = _configuration.GetValue<decimal>("PaymentSettings:AdminCommissionRate", 0.05m);
            return Math.Round(amount * commissionRate, 2);
        }

        private async Task<decimal> CalculateProviderAmountAsync(decimal amount)
        {
            var adminCommission = await CalculateAdminCommissionAsync(amount);
            return Math.Round(amount - adminCommission, 2);
        }

        private string GenerateTransactionId()
        {
            return $"TXN_{DateTime.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid().ToString("N")[..8].ToUpper()}";
        }

        private string GenerateSessionKey()
        {
            return $"SESS_{DateTime.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid().ToString("N")[..12].ToUpper()}";
        }
    }

    // SSL Commerz Response Models
    public class SSLCommerzResponse
    {
        public string Status { get; set; }
        public string FailedReason { get; set; }
        public string SessionKey { get; set; }
        public string GatewayPageURL { get; set; }
    }

    public class SSLCommerzVerificationResponse
    {
        public string Status { get; set; }
        public string TranDate { get; set; }
        public string TranId { get; set; }
        public string ValId { get; set; }
        public string Amount { get; set; }
        public string StoreAmount { get; set; }
        public string Currency { get; set; }
        public string BankTranId { get; set; }
        public string CardType { get; set; }
        public string CardNo { get; set; }
        public string CardIssuer { get; set; }
        public string CardBrand { get; set; }
        public string CardSubBrand { get; set; }
        public string CardIssuerCountry { get; set; }
        public string CardIssuerCountryCode { get; set; }
        public string StoreId { get; set; }
        public string VerifySign { get; set; }
        public string VerifyKey { get; set; }
        public string BaseFair { get; set; }
        public string ValueA { get; set; }
        public string ValueB { get; set; }
        public string ValueC { get; set; }
        public string ValueD { get; set; }
        public string ValueE { get; set; }
    }

    public class SSLCommerzRefundResponse
    {
        public string Status { get; set; }
        public string RefundRefId { get; set; }
        public string RefundAmount { get; set; }
        public string RefundDate { get; set; }
    }
}

