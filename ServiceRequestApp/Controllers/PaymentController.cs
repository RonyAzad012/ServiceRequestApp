using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ServiceRequestApp.Data;
using ServiceRequestApp.Models;
using ServiceRequestApp.Services;
using System.Security.Claims;

namespace ServiceRequestApp.Controllers
{
    [Authorize]
    public class PaymentController : Controller
    {
        private readonly IPaymentService _paymentService;
        private readonly ISSLCommerzPaymentService _sslCommerzService;
        private readonly ApplicationDbContext _dbContext;
        private readonly UserManager<ApplicationUser> _userManager;

        public PaymentController(
            IPaymentService paymentService,
            ISSLCommerzPaymentService sslCommerzService,
            ApplicationDbContext dbContext,
            UserManager<ApplicationUser> userManager)
        {
            _paymentService = paymentService;
            _sslCommerzService = sslCommerzService;
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

            ViewBag.ServiceRequest = serviceRequest;
            ViewBag.CurrentUser = currentUser;

            return View();
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
                CustomerName = $"{currentUser.FirstName} {currentUser.LastName}",
                CustomerEmail = currentUser.Email,
                CustomerPhone = currentUser.PhoneNumber,
                CustomerAddress = currentUser.Address,
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
                ModelState.AddModelError("", result.Message);
                ViewBag.ServiceRequest = serviceRequest;
                ViewBag.CurrentUser = currentUser;
                return View(model);
            }
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
        [HttpGet]
        public async Task<IActionResult> Success(string session)
        {
            var result = await _sslCommerzService.VerifyPaymentAsync(session, "VALID");
            
            if (result.Success)
            {
                TempData["PaymentSuccess"] = $"Payment of ৳{result.Amount:N2} completed successfully! Transaction ID: {result.TransactionId}";
                return RedirectToAction("Details", "ServiceRequest", new { id = result.AdditionalData["ServiceRequestId"] });
            }
            else
            {
                TempData["PaymentError"] = result.Message;
                return RedirectToAction("Index");
            }
        }

        [HttpGet]
        public async Task<IActionResult> Fail(string session)
        {
            var result = await _sslCommerzService.VerifyPaymentAsync(session, "FAILED");
            TempData["PaymentError"] = "Payment failed. Please try again.";
            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> Cancel(string session)
        {
            var result = await _sslCommerzService.VerifyPaymentAsync(session, "CANCELLED");
            TempData["PaymentMessage"] = "Payment was cancelled.";
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> IPN()
        {
            // SSL Commerz IPN (Instant Payment Notification) handler
            var form = await Request.ReadFormAsync();
            var sessionKey = form["value_a"].ToString();
            var status = form["status"].ToString();

            if (!string.IsNullOrEmpty(sessionKey))
            {
                var result = await _sslCommerzService.VerifyPaymentAsync(sessionKey, status);
                return Ok(new { success = result.Success, message = result.Message });
            }

            return BadRequest("Invalid IPN data");
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

    // View Models
    public class PaymentProcessViewModel
    {
        public int ServiceRequestId { get; set; }
        public decimal Amount { get; set; }
        public string PaymentMethod { get; set; } = "Card";
        
        // Card details
        public string? CardNumber { get; set; }
        public string? ExpiryDate { get; set; }
        public string? CVV { get; set; }
        public string? CardholderName { get; set; }
        
        // Mobile banking details
        public string? MobileNumber { get; set; }
        public string? Pin { get; set; }
    }

    public class RefundRequestViewModel
    {
        public string TransactionId { get; set; }
        public decimal Amount { get; set; }
        public string Reason { get; set; }
    }
}
