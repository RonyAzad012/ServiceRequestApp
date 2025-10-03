using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ServiceRequestApp.Data;
using ServiceRequestApp.Models;
using ServiceRequestApp.Services;
using ServiceRequestApp.ViewModels;
using System.Security.Claims;

namespace ServiceRequestApp.Controllers
{
    [Authorize]
    public class PaymentController : Controller
    {
        private readonly IPaymentService _paymentService;
        private readonly ISSLCommerzPaymentService _sslCommerzService;
        private readonly IPaymentCompletionService _paymentCompletionService;
        private readonly IInvoiceService _invoiceService;
        private readonly ApplicationDbContext _dbContext;
        private readonly UserManager<ApplicationUser> _userManager;

        public PaymentController(
            IPaymentService paymentService,
            ISSLCommerzPaymentService sslCommerzService,
            IPaymentCompletionService paymentCompletionService,
            IInvoiceService invoiceService,
            ApplicationDbContext dbContext,
            UserManager<ApplicationUser> userManager)
        {
            _paymentService = paymentService;
            _sslCommerzService = sslCommerzService;
            _paymentCompletionService = paymentCompletionService;
            _invoiceService = invoiceService;
            _dbContext = dbContext;
            _userManager = userManager;
        }

        // GET: Payment/Index - Payment dashboard
        public async Task<IActionResult> Index()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            
            // Get user's payment transactions
            var transactions = await _dbContext.PaymentTransactions
                .Where(pt => pt.UserId == currentUser.Id)
                .Include(pt => pt.ServiceRequest)
                .OrderByDescending(pt => pt.TransactionDate)
                .Take(20)
                .ToListAsync();

            // Get payment statistics
            var totalPaid = await _dbContext.PaymentTransactions
                .Where(pt => pt.UserId == currentUser.Id && pt.Amount > 0)
                .SumAsync(pt => pt.Amount);

            var totalRefunded = await _dbContext.PaymentTransactions
                .Where(pt => pt.UserId == currentUser.Id && pt.Amount < 0)
                .SumAsync(pt => Math.Abs(pt.Amount));

            ViewBag.TotalPaid = totalPaid;
            ViewBag.TotalRefunded = totalRefunded;
            ViewBag.CurrentUser = currentUser;

            return View(transactions);
        }

        // GET: Payment/Process/{serviceRequestId}
        public async Task<IActionResult> Process(int serviceRequestId)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var serviceRequest = await _dbContext.ServiceRequests
                .Include(sr => sr.Requester)
                .Include(sr => sr.Provider)
                .FirstOrDefaultAsync(sr => sr.Id == serviceRequestId);

            if (serviceRequest == null)
            {
                return NotFound();
            }

            // Check if user is authorized to make payment
            if (serviceRequest.RequesterId != currentUser.Id)
            {
                return Forbid();
            }

            // Check if payment is already made
            if (serviceRequest.PaymentStatus == "Paid")
            {
                TempData["PaymentMessage"] = "Payment has already been made for this request.";
                return RedirectToAction("Details", "ServiceRequest", new { id = serviceRequestId });
            }

            // Auto-initiate SSLCommerz payment session and redirect to gateway (sandbox in test mode)
            var amountToCharge = serviceRequest.Budget ?? 0;
            if (amountToCharge <= 0)
            {
                // Fallback for sandbox if no budget set
                amountToCharge = 10.00m;
            }
            var paymentRequest = new PaymentRequest
            {
                UserId = currentUser.Id,
                ServiceRequestId = serviceRequest.Id,
                Amount = amountToCharge,
                PaymentMethod = "Card",
                CustomerName = string.IsNullOrWhiteSpace(currentUser.FirstName + currentUser.LastName) ? "Customer" : $"{currentUser.FirstName} {currentUser.LastName}".Trim(),
                CustomerEmail = string.IsNullOrWhiteSpace(currentUser.Email) ? "test@example.com" : currentUser.Email,
                CustomerPhone = string.IsNullOrWhiteSpace(currentUser.PhoneNumber) ? (serviceRequest.Requester?.PhoneNumber ?? "01700000000") : currentUser.PhoneNumber,
                CustomerAddress = string.IsNullOrWhiteSpace(currentUser.Address) ? (serviceRequest.Address ?? "Dhaka") : currentUser.Address
            };

            var initResult = await _sslCommerzService.CreatePaymentSessionAsync(paymentRequest);
            if (initResult.Success && initResult.AdditionalData.ContainsKey("GatewayURL"))
            {
                return Redirect(initResult.AdditionalData["GatewayURL"].ToString());
            }

            // Fallback to existing view with error if gateway init fails
            TempData["PaymentError"] = string.IsNullOrWhiteSpace(initResult.Message) ? "Could not initiate payment. Please try again." : initResult.Message;

            ViewBag.ServiceRequest = serviceRequest;
            ViewBag.CurrentUser = currentUser;
            var model = new PaymentProcessViewModel
            {
                ServiceRequestId = serviceRequest.Id,
                Amount = amountToCharge,
                PaymentMethod = "Card"
            };
            return View(model);
        }

        // GET: Payment/Start/{serviceRequestId}
        [HttpGet]
        public async Task<IActionResult> Start(int serviceRequestId)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var serviceRequest = await _dbContext.ServiceRequests
                .Include(sr => sr.Requester)
                .FirstOrDefaultAsync(sr => sr.Id == serviceRequestId);

            if (serviceRequest == null)
            {
                return NotFound();
            }

            if (serviceRequest.RequesterId != currentUser.Id)
            {
                return Forbid();
            }

            if (serviceRequest.PaymentStatus == "Paid")
            {
                TempData["PaymentMessage"] = "Payment has already been made for this request.";
                return RedirectToAction("Details", "ServiceRequest", new { id = serviceRequestId });
            }

            var amountToCharge = serviceRequest.Budget ?? 0m;
            if (amountToCharge <= 0)
            {
                amountToCharge = 10.00m; // sandbox fallback
            }

            var paymentRequest = new PaymentRequest
            {
                UserId = currentUser.Id,
                ServiceRequestId = serviceRequest.Id,
                Amount = amountToCharge,
                PaymentMethod = "Card",
                CustomerName = string.IsNullOrWhiteSpace(currentUser.FirstName + currentUser.LastName) ? "Customer" : ($"{currentUser.FirstName} {currentUser.LastName}").Trim(),
                CustomerEmail = string.IsNullOrWhiteSpace(currentUser.Email) ? "test@example.com" : currentUser.Email,
                CustomerPhone = string.IsNullOrWhiteSpace(currentUser.PhoneNumber) ? (serviceRequest.Requester?.PhoneNumber ?? "01700000000") : currentUser.PhoneNumber,
                CustomerAddress = string.IsNullOrWhiteSpace(currentUser.Address) ? (serviceRequest.Address ?? "Dhaka") : currentUser.Address,
                AdditionalData = new Dictionary<string, string>
                {
                    ["baseUrl"] = $"{Request.Scheme}://{Request.Host}"
                }
            };

            var result = await _sslCommerzService.CreatePaymentSessionAsync(paymentRequest);
            if (result.Success && result.AdditionalData.ContainsKey("GatewayURL"))
            {
                return Redirect(result.AdditionalData["GatewayURL"].ToString());
            }

            TempData["PaymentError"] = string.IsNullOrWhiteSpace(result.Message) ? "Could not initiate payment. Please try again." : result.Message;
            return RedirectToAction("Details", "ServiceRequest", new { id = serviceRequestId });
        }

        // POST: Payment/Process
        [HttpPost]
        public async Task<IActionResult> Process(PaymentProcessViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var currentUser = await _userManager.GetUserAsync(User);
            var serviceRequest = await _dbContext.ServiceRequests
                .Include(sr => sr.Requester)
                .FirstOrDefaultAsync(sr => sr.Id == model.ServiceRequestId);

            if (serviceRequest == null)
            {
                return NotFound();
            }

            // Create payment request for SSL Commerz
            var paymentRequest = new PaymentRequest
            {
                UserId = currentUser.Id,
                ServiceRequestId = model.ServiceRequestId,
                Amount = model.Amount,
                PaymentMethod = model.PaymentMethod,
                CustomerName = string.IsNullOrWhiteSpace(currentUser.FirstName + currentUser.LastName) ? "Customer" : $"{currentUser.FirstName} {currentUser.LastName}".Trim(),
                CustomerEmail = string.IsNullOrWhiteSpace(currentUser.Email) ? "test@example.com" : currentUser.Email,
                CustomerPhone = string.IsNullOrWhiteSpace(currentUser.PhoneNumber) ? (serviceRequest.Requester?.PhoneNumber ?? "01700000000") : currentUser.PhoneNumber,
                CustomerAddress = string.IsNullOrWhiteSpace(currentUser.Address) ? (serviceRequest.Address ?? "Dhaka") : currentUser.Address,
                AdditionalData = new Dictionary<string, string>
                {
                    ["cardNumber"] = model.CardNumber?.Replace(" ", ""),
                    ["expiryDate"] = model.ExpiryDate,
                    ["cvv"] = model.CVV,
                    ["cardholderName"] = model.CardholderName
                }
            };

            // Process payment with SSL Commerz
            var result = await _sslCommerzService.CreatePaymentSessionAsync(paymentRequest);

            if (result.Success && result.AdditionalData.ContainsKey("GatewayURL"))
            {
                // Redirect to SSL Commerz payment gateway
                return Redirect(result.AdditionalData["GatewayURL"].ToString());
            }
            else
            {
                TempData["PaymentError"] = string.IsNullOrWhiteSpace(result.Message) ? "Failed to create payment session. Please check your details and try again." : result.Message;
                return RedirectToAction("Process", new { serviceRequestId = model.ServiceRequestId });
            }
        }

        // GET: Payment/Invoice/{serviceRequestId}
        [HttpGet]
        public async Task<IActionResult> Invoice(int serviceRequestId)
        {
            var currentUser = await _userManager.GetUserAsync(User);

            var serviceRequest = await _dbContext.ServiceRequests
                .Include(sr => sr.Requester)
                .Include(sr => sr.Provider)
                .FirstOrDefaultAsync(sr => sr.Id == serviceRequestId);

            if (serviceRequest == null)
            {
                return NotFound();
            }

            // Ensure only request owner or admin can view invoice
            if (serviceRequest.RequesterId != currentUser.Id && !User.IsInRole("Admin"))
            {
                return Forbid();
            }

            var transaction = await _dbContext.PaymentTransactions
                .Where(pt => pt.ServiceRequestId == serviceRequestId && pt.Status == "Completed")
                .OrderByDescending(pt => pt.TransactionDate)
                .FirstOrDefaultAsync();

            if (transaction == null)
            {
                TempData["PaymentError"] = "No completed payment found for this request.";
                return RedirectToAction("Details", "ServiceRequest", new { id = serviceRequestId });
            }

            ViewBag.ServiceRequest = serviceRequest;
            return View(transaction);
        }

        // GET: Payment/Verify/{transactionId}
        public async Task<IActionResult> Verify(string transactionId)
        {
            var result = await _paymentService.VerifyPaymentAsync(transactionId);
            
            if (result.Success)
            {
                TempData["PaymentMessage"] = "Payment verified successfully!";
            }
            else
            {
                TempData["PaymentError"] = result.Message;
            }

            return RedirectToAction("Index");
        }

        // POST: Payment/Refund
        [HttpPost]
        public async Task<IActionResult> Refund(RefundRequestViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "Invalid refund request" });
            }

            var currentUser = await _userManager.GetUserAsync(User);
            
            // Check if user is admin or the original payer
            var transaction = await _dbContext.PaymentTransactions
                .FirstOrDefaultAsync(pt => pt.TransactionId == model.TransactionId);

            if (transaction == null)
            {
                return Json(new { success = false, message = "Transaction not found" });
            }

            if (transaction.UserId != currentUser.Id && !User.IsInRole("Admin"))
            {
                return Json(new { success = false, message = "Unauthorized" });
            }

            var result = await _paymentService.RefundPaymentAsync(model.TransactionId, model.Amount);

            return Json(new { 
                success = result.Success, 
                message = result.Message,
                transactionId = result.TransactionId
            });
        }

        // API: Get payment history
        [HttpGet]
        public async Task<IActionResult> GetPaymentHistory(int page = 1, int pageSize = 10)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            
            var transactions = await _dbContext.PaymentTransactions
                .Where(pt => pt.UserId == currentUser.Id)
                .Include(pt => pt.ServiceRequest)
                .OrderByDescending(pt => pt.TransactionDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(pt => new {
                    pt.Id,
                    pt.TransactionId,
                    pt.Amount,
                    pt.Currency,
                    pt.TransactionDate,
                    pt.Status,
                    pt.PaymentMethod,
                    pt.Remarks,
                    ServiceRequestTitle = pt.ServiceRequest.Title
                })
                .ToListAsync();

            return Json(new { success = true, transactions });
        }

        // SSL Commerz Callback Actions
        [AllowAnonymous]
        [AcceptVerbs("GET", "POST")]
        public async Task<IActionResult> Success(string val_id, string status, string tran_id, string ssl_id, string sessionkey)
        {
            // Handle both GET and POST parameters from SSLCommerz
            if (string.IsNullOrEmpty(val_id))
            {
                val_id = Request.Query["val_id"].FirstOrDefault() ?? Request.Form["val_id"].FirstOrDefault();
            }
            if (string.IsNullOrEmpty(status))
            {
                status = Request.Query["status"].FirstOrDefault() ?? Request.Form["status"].FirstOrDefault() ?? "VALID";
            }
            if (string.IsNullOrEmpty(tran_id))
            {
                tran_id = Request.Query["tran_id"].FirstOrDefault() ?? Request.Form["tran_id"].FirstOrDefault();
            }
            if (string.IsNullOrEmpty(ssl_id))
            {
                ssl_id = Request.Query["ssl_id"].FirstOrDefault() ?? Request.Form["ssl_id"].FirstOrDefault();
            }
            if (string.IsNullOrEmpty(sessionkey))
            {
                sessionkey = Request.Query["SESSIONKEY"].FirstOrDefault() ?? Request.Form["SESSIONKEY"].FirstOrDefault();
            }

            // Log all callback parameters for debugging
            System.Diagnostics.Debug.WriteLine($"Payment Success Callback - val_id: {val_id}, status: {status}, tran_id: {tran_id}, ssl_id: {ssl_id}, sessionkey: {sessionkey}");
            System.Diagnostics.Debug.WriteLine($"All Query Parameters: {string.Join(", ", Request.Query.Select(q => $"{q.Key}={q.Value}"))}");
            
            // Try to find the transaction using ssl_id or tran_id
            string transactionId = null;
            if (!string.IsNullOrEmpty(ssl_id))
            {
                transactionId = ssl_id;
            }
            else if (!string.IsNullOrEmpty(tran_id))
            {
                transactionId = tran_id;
            }
            else if (!string.IsNullOrEmpty(val_id))
            {
                transactionId = val_id;
            }

            if (string.IsNullOrEmpty(transactionId))
            {
                TempData["PaymentError"] = "Invalid payment callback - no transaction identifier found";
                return RedirectToAction("Index", "Home");
            }

            // For SSLCommerz sandbox, we'll directly complete the payment since we have the transaction ID
            var result = await _sslCommerzService.VerifyPaymentAsync(transactionId, "VALID");
            
            // Log the verification result for debugging
            System.Diagnostics.Debug.WriteLine($"Payment Verification Result - Success: {result.Success}, Message: {result.Message}");
            
            if (result.Success)
            {
                var adminCommission = result.AdminCommission;
                var providerAmount = result.ProviderAmount;
                var totalAmount = result.Amount;
                
                var srId = result.AdditionalData.ContainsKey("ServiceRequestId") ? result.AdditionalData["ServiceRequestId"] : null;
                if (srId != null)
                {
                    // Generate invoice for the completed payment
                    var invoiceResult = await _invoiceService.GenerateInvoiceAsync((int)srId, result.TransactionId);
                    
                    if (invoiceResult.Success)
                    {
                        TempData["PaymentSuccess"] = $"Payment of ৳{totalAmount:N2} completed successfully! " +
                            $"Admin Commission: ৳{adminCommission:N2}, Provider Amount: ৳{providerAmount:N2}. " +
                            $"Transaction ID: {result.TransactionId}. Invoice: {invoiceResult.Invoice?.InvoiceNumber}";
                        
                        // Redirect to payment success page with invoice details
                        return RedirectToAction("PaymentSuccess", new { 
                            serviceRequestId = srId, 
                            invoiceId = invoiceResult.Invoice?.Id,
                            transactionId = result.TransactionId 
                        });
                    }
                    else
                    {
                        TempData["PaymentError"] = $"Payment completed but invoice generation failed: {invoiceResult.Message}";
                        return RedirectToAction("Details", "ServiceRequest", new { id = srId });
                    }
                }
                
                TempData["PaymentSuccess"] = $"Payment of ৳{totalAmount:N2} completed successfully! " +
                    $"Admin Commission: ৳{adminCommission:N2}, Provider Amount: ৳{providerAmount:N2}. " +
                    $"Transaction ID: {result.TransactionId}";
                return RedirectToAction("Index");
            }
            else
            {
                // Log the error for debugging
                System.Diagnostics.Debug.WriteLine($"Payment verification failed: {result.Message}");
                
                // Provide user-friendly error message
                var userFriendlyMessage = "Payment verification failed. Please try again or contact support if the issue persists.";
                TempData["PaymentError"] = userFriendlyMessage;
                
                // Try to redirect back to the service request details if we have the service request ID
                var srId = result.AdditionalData?.ContainsKey("ServiceRequestId") == true ? result.AdditionalData["ServiceRequestId"] : null;
                if (srId != null)
                {
                    return RedirectToAction("Details", "ServiceRequest", new { id = srId });
                }
                
                return RedirectToAction("Index");
            }
        }

        [AllowAnonymous]
        [AcceptVerbs("GET", "POST")]
        public async Task<IActionResult> Fail(string val_id)
        {
            // Handle both GET and POST parameters from SSLCommerz
            if (string.IsNullOrEmpty(val_id))
            {
                val_id = Request.Query["val_id"].FirstOrDefault() ?? Request.Form["val_id"].FirstOrDefault();
            }

            if (!string.IsNullOrEmpty(val_id))
            {
                var result = await _sslCommerzService.VerifyPaymentAsync(val_id, "FAILED");
            }
            
            TempData["PaymentError"] = "Payment failed. Please try again.";
            return RedirectToAction("Index", "Home");
        }

        [AllowAnonymous]
        [AcceptVerbs("GET", "POST")]
        public async Task<IActionResult> Cancel(string val_id)
        {
            // Handle both GET and POST parameters from SSLCommerz
            if (string.IsNullOrEmpty(val_id))
            {
                val_id = Request.Query["val_id"].FirstOrDefault() ?? Request.Form["val_id"].FirstOrDefault();
            }

            if (!string.IsNullOrEmpty(val_id))
            {
                var result = await _sslCommerzService.VerifyPaymentAsync(val_id, "CANCELLED");
            }
            
            TempData["PaymentMessage"] = "Payment was cancelled.";
            return RedirectToAction("Index", "Home");
        }

        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> IPN()
        {
            // SSL Commerz IPN (Instant Payment Notification) handler
            var form = await Request.ReadFormAsync();
            var valId = form["val_id"].ToString();
            var status = form["status"].ToString();
            var tranId = form["tran_id"].ToString();

            if (!string.IsNullOrEmpty(valId))
            {
                var result = await _sslCommerzService.VerifyPaymentAsync(valId, status);
                return Ok(new { success = result.Success, message = result.Message });
            }

            return BadRequest("Invalid IPN data");
        }

        // GET: Payment/PaymentSuccess - Payment completion success page
        [HttpGet]
        public async Task<IActionResult> PaymentSuccess(int serviceRequestId, int? invoiceId, string transactionId)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            
            // Get service request details
            var serviceRequest = await _dbContext.ServiceRequests
                .Include(sr => sr.Requester)
                .Include(sr => sr.Provider)
                .Include(sr => sr.Category)
                .Include(sr => sr.AcceptedRequest)
                .ThenInclude(ar => ar.Provider)
                .FirstOrDefaultAsync(sr => sr.Id == serviceRequestId);

            if (serviceRequest == null)
            {
                TempData["PaymentError"] = "Service request not found";
                return RedirectToAction("Index");
            }

            // Get invoice if provided
            Invoice? invoice = null;
            if (invoiceId.HasValue)
            {
                var invoiceResult = await _invoiceService.GetInvoiceAsync(invoiceId.Value);
                if (invoiceResult.Success)
                {
                    invoice = invoiceResult.Invoice;
                }
            }

            // Get payment transaction
            var transaction = await _dbContext.PaymentTransactions
                .FirstOrDefaultAsync(pt => pt.TransactionId == transactionId);

            ViewBag.ServiceRequest = serviceRequest;
            ViewBag.Invoice = invoice;
            ViewBag.Transaction = transaction;
            ViewBag.CurrentUser = currentUser;

            return View();
        }

        // GET: Payment/Invoice/{invoiceId} - View specific invoice
        [HttpGet]
        public async Task<IActionResult> ViewInvoice(int invoiceId)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            
            var invoiceResult = await _invoiceService.GetInvoiceAsync(invoiceId);
            if (!invoiceResult.Success)
            {
                TempData["PaymentError"] = invoiceResult.Message;
                return RedirectToAction("Index");
            }

            var invoice = invoiceResult.Invoice;
            
            // Check if user has access to this invoice
            if (invoice.RequesterId != currentUser.Id && invoice.ProviderId != currentUser.Id && !User.IsInRole("Admin"))
            {
                return Forbid();
            }

            ViewBag.Invoice = invoice;
            ViewBag.CurrentUser = currentUser;

            return View();
        }

        // GET: Payment/MyInvoices - User's invoices
        [HttpGet]
        public async Task<IActionResult> MyInvoices()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            
            var invoiceResult = await _invoiceService.GetInvoicesForUserAsync(currentUser.Id);
            if (!invoiceResult.Success)
            {
                TempData["PaymentError"] = invoiceResult.Message;
                return RedirectToAction("Index");
            }

            ViewBag.Invoices = invoiceResult.Invoices;
            ViewBag.CurrentUser = currentUser;

            return View();
        }

        // DEBUG: Manual payment completion test
        [HttpGet]
        public async Task<IActionResult> TestPaymentCompletion(string transactionId)
        {
            if (string.IsNullOrEmpty(transactionId))
            {
                return Json(new { success = false, message = "Transaction ID required" });
            }

            try
            {
                var transaction = await _dbContext.PaymentTransactions
                    .Include(pt => pt.ServiceRequest)
                    .FirstOrDefaultAsync(pt => pt.TransactionId == transactionId);

                if (transaction == null)
                {
                    return Json(new { success = false, message = "Transaction not found" });
                }

                // Manually complete the payment
                var completionResult = await _paymentCompletionService.CompletePaymentAsync(transactionId, "TEST_VAL_ID");

                return Json(new { 
                    success = completionResult.Success, 
                    message = completionResult.Message,
                    transactionId = completionResult.TransactionId,
                    adminCommission = completionResult.AdminCommission,
                    providerAmount = completionResult.ProviderAmount,
                    totalAmount = completionResult.TotalAmount
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // API: Get payment statistics
        [HttpGet]
        public async Task<IActionResult> GetPaymentStats()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            
            var totalPaid = await _dbContext.PaymentTransactions
                .Where(pt => pt.UserId == currentUser.Id && pt.Amount > 0)
                .SumAsync(pt => pt.Amount);

            var totalRefunded = await _dbContext.PaymentTransactions
                .Where(pt => pt.UserId == currentUser.Id && pt.Amount < 0)
                .SumAsync(pt => Math.Abs(pt.Amount));

            var monthlyStats = await _dbContext.PaymentTransactions
                .Where(pt => pt.UserId == currentUser.Id && 
                           pt.TransactionDate >= DateTime.UtcNow.AddMonths(-12) &&
                           pt.Amount > 0)
                .GroupBy(pt => new { pt.TransactionDate.Year, pt.TransactionDate.Month })
                .Select(g => new {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    Amount = g.Sum(pt => pt.Amount),
                    Count = g.Count()
                })
                .OrderBy(s => s.Year)
                .ThenBy(s => s.Month)
                .ToListAsync();

            return Json(new { 
                success = true, 
                totalPaid, 
                totalRefunded,
                monthlyStats
            });
        }
    }

}
