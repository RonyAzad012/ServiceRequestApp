using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ServiceRequestApp.Data;
using ServiceRequestApp.Models;
using ServiceRequestApp.ViewModels;

namespace ServiceRequestApp.Controllers
{
    //Authorize attribute is used to restrict access to the controller or its actions to only authenticated users.
    [Authorize]
    public class ServiceRequestController : Controller
    {
        //The ApplicationDbContext and UserManager<ApplicationUser> services are injected into the controller.
        private readonly ApplicationDbContext _dbContext;
        private readonly UserManager<ApplicationUser> _userManager;

        //Dependency injection is used to inject the ApplicationDbContext and UserManager<ApplicationUser> services into the controller.
        public ServiceRequestController(
            ApplicationDbContext context,//For database operations
            UserManager<ApplicationUser> userManager)//For user management
        {
            _dbContext = context;
            _userManager = userManager;
        }

        // GET: ServiceRequest
        // The Index action method returns a list of service requests based on the user type.
        // If the user is a provider, the action method returns a list of available requests and accepted requests.
        // If the user is a requester, the action method returns a list of requests created by the user.
       
        public async Task<IActionResult> Index()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var requests = new List<ServiceRequest>();

            if (currentUser.UserType == "Provider")
            {
                // Providers see available requests and their accepted requests
                requests = await _dbContext.ServiceRequests
                    .Include(r => r.Requester)
                    .Include(r => r.AcceptedRequest)
                    .Where(r => r.Status == "Pending" ||
                           (r.AcceptedRequest != null &&
                            r.AcceptedRequest.ProviderId == currentUser.Id))
                    .ToListAsync();
            }
            else if (currentUser.UserType == "Admin")
            {
                // Admins see all requests
                requests = await _dbContext.ServiceRequests
                    .Include(r => r.Requester)
                    .Include(r => r.AcceptedRequest)
                    .ToListAsync();
            }
            else
            {
                // Requesters see only their own requests
                requests = await _dbContext.ServiceRequests
                    .Include(r => r.Requester)
                    .Include(r => r.AcceptedRequest)
                    .Where(r => r.RequesterId == currentUser.Id)
                    .ToListAsync();
            }

            return View(requests);
        }

        // GET: ServiceRequest/Create
        //return views for a new service request
        [Authorize(Roles = "Requester")]
        public IActionResult Create()
        {
            return View();
        }

        // POST: ServiceRequest/Create
        //Process form submission if the model is valid and the user is a requester create a new service request and save it to the database.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Requester")]
        public async Task<IActionResult> Create(CreateServiceRequestViewModel model)
        {
            if (ModelState.IsValid)
            {
                var currentUser = await _userManager.GetUserAsync(User);
                string imagePaths = null;
                if (model.Images != null && model.Images.Count > 0)
                {
                    var uploads = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/requests");
                    Directory.CreateDirectory(uploads);
                    var fileNames = new List<string>();
                    foreach (var file in model.Images)
                    {
                        if (file.Length > 0)
                        {
                            var fileName = Guid.NewGuid() + Path.GetExtension(file.FileName);
                            var filePath = Path.Combine(uploads, fileName);
                            using (var stream = new FileStream(filePath, FileMode.Create))
                            {
                                await file.CopyToAsync(stream);
                            }
                            fileNames.Add("/images/requests/" + fileName);
                        }
                    }
                    imagePaths = string.Join(",", fileNames);
                }
                var request = new ServiceRequest
                {
                    Title = model.Title,
                    Description = model.Description,
                    ServiceType = model.ServiceType,
                    CreatedAt = DateTime.UtcNow,
                    Status = "Pending",
                    Latitude = model.Latitude,
                    Longitude = model.Longitude,
                    RequesterId = currentUser.Id,
                    Address = model.Address,
                    Zipcode = model.Zipcode,
                    Price = model.Price,
                    PhoneNumber = model.PhoneNumber,
                    Deadline = model.Deadline,
                    ImagePaths = imagePaths
                };
                _dbContext.Add(request);
                await _dbContext.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(model);
        }

        // POST: ServiceRequest/Accept/5
        //Allows a provider to accept a service request if the request is still pending provider can accept the request and change the status to accepted.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Provider")]
        public async Task<IActionResult> Accept(int id)
        {
            var request = await _dbContext.ServiceRequests
                .Include(r => r.AcceptedRequest)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (request == null)
            {
                return NotFound();
            }

            if (request.Status != "Pending")
            {
                return BadRequest("This request is no longer available.");
            }

            var currentUser = await _userManager.GetUserAsync(User);

            var acceptedRequest = new AcceptedRequest
            {
                ServiceRequestId = request.Id,
                ProviderId = currentUser.Id,
                AcceptedAt = DateTime.UtcNow,
                Status = "InProgress"
            };

            request.Status = "Accepted";
            _dbContext.AcceptedRequests.Add(acceptedRequest);
            await _dbContext.SaveChangesAsync();

            // TODO: Send notification to requester

            return RedirectToAction(nameof(Index));
        }

        // GET: ServiceRequest/Details/5
        //Returns the details of a service request based on the request ID.
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var request = await _dbContext.ServiceRequests
                .Include(r => r.Requester)
                .Include(r => r.AcceptedRequest)
                    .ThenInclude(ar => ar.Provider)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (request == null)
            {
                return NotFound();
            }

            return View(request);
        }

        // POST: ServiceRequest/Complete/5
        //Allows the provider or requester to mark a service request as completed.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Complete(int id)
        {
            var request = await _dbContext.ServiceRequests
                .Include(r => r.AcceptedRequest)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (request == null)
            {
                return NotFound();
            }

            var currentUser = await _userManager.GetUserAsync(User);

            // Check if the current user is either the provider or requester
            bool isProvider = request.AcceptedRequest?.ProviderId == currentUser.Id;
            bool isRequester = request.RequesterId == currentUser.Id;

            if (!isProvider && !isRequester)
            {
                return Unauthorized();
            }

            // Only allow completion if the request is in "Accepted" status or partially completed
            if (request.Status != "Accepted" &&
                request.Status != "ProviderCompleted" &&
                request.Status != "RequesterCompleted")
            {
                return BadRequest("Request cannot be completed at this time.");
            }

            if (isProvider && request.Status != "RequesterCompleted")
            {
                request.Status = "ProviderCompleted";
                if (request.AcceptedRequest != null)
                {
                    request.AcceptedRequest.Status = "ProviderCompleted";
                }
            }
            else if (isRequester && request.Status != "ProviderCompleted")
            {
                request.Status = "RequesterCompleted";
                if (request.AcceptedRequest != null)
                {
                    request.AcceptedRequest.Status = "RequesterCompleted";
                }
            }

            // Check if both parties have marked it as complete
            if ((request.Status == "ProviderCompleted" && request.AcceptedRequest?.Status == "RequesterCompleted") ||
                (request.Status == "RequesterCompleted" && request.AcceptedRequest?.Status == "ProviderCompleted"))
            {
                request.Status = "Completed";
                request.AcceptedRequest.Status = "Completed";
            }

            await _dbContext.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET: ServiceRequest/Edit/5
        //Returns the view for editing a service request based on the request ID.
        [Authorize(Roles = "Requester")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var currentUser = await _userManager.GetUserAsync(User);
            var request = await _dbContext.ServiceRequests.FindAsync(id);

            if (request == null)
            {
                return NotFound();
            }

            // Only allow editing if the request belongs to the current user and is still pending
            if (request.RequesterId != currentUser.Id || request.Status != "Pending")
            {
                return Unauthorized();
            }

            var viewModel = new EditServiceRequestViewModel
            {
                Title = request.Title,
                Description = request.Description
            };

            return View(viewModel);
        }

        // POST: ServiceRequest/Edit/5
        //Process form submission if the model is valid and the user is a requester update the service request and save it to the database.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Requester")]
        public async Task<IActionResult> Edit(int id, EditServiceRequestViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var currentUser = await _userManager.GetUserAsync(User);
            var existingRequest = await _dbContext.ServiceRequests.FindAsync(id);

            if (existingRequest == null)
            {
                return NotFound();
            }

            if (existingRequest.RequesterId != currentUser.Id || existingRequest.Status != "Pending")
            {
                return Unauthorized();
            }

            try
            {
                existingRequest.Title = model.Title;
                existingRequest.Description = model.Description;

                await _dbContext.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ServiceRequestExists(id))
                {
                    return NotFound();
                }
                throw;
            }
        }

        // GET: ServiceRequest/Delete/5
        //Returns the view for deleting a service request based on the request ID.
        [Authorize(Roles = "Requester")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var currentUser = await _userManager.GetUserAsync(User);
            var request = await _dbContext.ServiceRequests
                .Include(r => r.Requester)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (request == null)
            {
                return NotFound();
            }

            // Only allow deletion if the request belongs to the current user and is still pending
            if (request.RequesterId != currentUser.Id || request.Status != "Pending")
            {
                return Unauthorized();
            }

            return View(request);
        }

        // POST: ServiceRequest/Delete/5
        //Process form submission if the user is a requester delete the service request and save it to the database.
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Requester")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var request = await _dbContext.ServiceRequests.FindAsync(id);

            if (request == null)
            {
                return NotFound();
            }

            // Only allow deletion if the request belongs to the current user and is still pending
            if (request.RequesterId != currentUser.Id || request.Status != "Pending")
            {
                return Unauthorized();
            }

            _dbContext.ServiceRequests.Remove(request);
            await _dbContext.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        //Check if a service request exists based on the request ID.
        private bool ServiceRequestExists(int id)
        {
            return _dbContext.ServiceRequests.Any(e => e.Id == id);
        }

        // GET: ServiceRequest/Pay/5
        //Initiate payment for a service request
        [Authorize]
        public async Task<IActionResult> Pay(int id)
        {
            var request = await _dbContext.ServiceRequests.FindAsync(id);
            if (request == null || request.Status != "Accepted" || request.PaymentStatus == "Paid")
            {
                return BadRequest("Payment not allowed.");
            }
            // Show payment options (stub)
            ViewBag.RequestId = id;
            ViewBag.Amount = request.Price;
            return View();
        }

        // POST: ServiceRequest/Pay/5
        //Process payment using the selected gateway
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Pay(int id, string gateway)
        {
            var request = await _dbContext.ServiceRequests.FindAsync(id);
            if (request == null || request.Status != "Accepted" || request.PaymentStatus == "Paid")
            {
                return BadRequest("Payment not allowed.");
            }
            // Simulate payment gateway redirect
            // In real integration, redirect to gateway and handle callback
            // For stub, redirect to callback with simulated transaction
            return RedirectToAction("PaymentCallback", new { id = id, gateway = gateway });
        }

        // GET: ServiceRequest/PaymentCallback
        //Simulate payment callback from the gateway
        [Authorize]
        public async Task<IActionResult> PaymentCallback(int id, string gateway)
        {
            var request = await _dbContext.ServiceRequests.FindAsync(id);
            if (request == null || request.Status != "Accepted" || request.PaymentStatus == "Paid")
            {
                return BadRequest("Payment not allowed.");
            }
            // Simulate payment success
            request.PaymentStatus = "Paid";
            request.PaymentTransactionId = $"{gateway.ToUpper()}-{Guid.NewGuid()}";
            request.PaymentAmount = request.Price;
            request.PaymentDate = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync();
            TempData["PaymentSuccess"] = $"Payment successful via {gateway}.";
            return RedirectToAction("Details", new { id = id });
        }

        // GET: ServiceRequest/AddReview/5
        [Authorize(Roles = "Requester")]
        public async Task<IActionResult> AddReview(int id)
        {
            var request = await _dbContext.ServiceRequests
                .Include(r => r.AcceptedRequest)
                .FirstOrDefaultAsync(r => r.Id == id);
            var currentUser = await _userManager.GetUserAsync(User);
            if (request == null || request.Status != "Completed" || request.RequesterId != currentUser.Id)
            {
                return BadRequest("Review not allowed.");
            }
            // Prevent duplicate review
            var existingReview = await _dbContext.Reviews.FirstOrDefaultAsync(r => r.ServiceRequestId == id);
            if (existingReview != null)
            {
                TempData["ReviewMessage"] = "You have already submitted a review.";
                return RedirectToAction("Details", new { id });
            }
            ViewBag.RequestId = id;
            ViewBag.ProviderName = request.AcceptedRequest?.Provider?.FirstName + " " + request.AcceptedRequest?.Provider?.LastName;
            return View();
        }

        // POST: ServiceRequest/AddReview/5
        [HttpPost]
        [Authorize(Roles = "Requester")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddReview(int id, int rating, string comment)
        {
            var request = await _dbContext.ServiceRequests
                .Include(r => r.AcceptedRequest)
                .FirstOrDefaultAsync(r => r.Id == id);
            var currentUser = await _userManager.GetUserAsync(User);
            if (request == null || request.Status != "Completed" || request.RequesterId != currentUser.Id)
            {
                return BadRequest("Review not allowed.");
            }
            // Prevent duplicate review
            var existingReview = await _dbContext.Reviews.FirstOrDefaultAsync(r => r.ServiceRequestId == id);
            if (existingReview != null)
            {
                TempData["ReviewMessage"] = "You have already submitted a review.";
                return RedirectToAction("Details", new { id });
            }
            var review = new Review
            {
                ServiceRequestId = id,
                ReviewerId = currentUser.Id,
                RevieweeId = request.AcceptedRequest.ProviderId,
                Rating = rating,
                Comment = comment,
                CreatedAt = DateTime.UtcNow
            };
            _dbContext.Reviews.Add(review);
            await _dbContext.SaveChangesAsync();
            TempData["ReviewMessage"] = "Review submitted successfully.";
            return RedirectToAction("Details", new { id });
        }

        // GET: ServiceRequest/ProviderReviews/{providerId}
        [AllowAnonymous]
        public async Task<IActionResult> ProviderReviews(string providerId)
        {
            if (string.IsNullOrEmpty(providerId))
                return NotFound();
            var provider = await _userManager.Users.FirstOrDefaultAsync(u => u.Id == providerId && u.UserType == "Provider");
            if (provider == null)
                return NotFound();
            var reviews = await _dbContext.Reviews
                .Where(r => r.RevieweeId == providerId)
                .OrderByDescending(r => r.CreatedAt)
                .Include(r => r.Reviewer)
                .ToListAsync();
            double avgRating = reviews.Count > 0 ? reviews.Average(r => r.Rating) : 0;
            ViewBag.Provider = provider;
            ViewBag.AvgRating = avgRating;
            return View(reviews);
        }

        // GET: ServiceRequest/ProviderDashboard
        [Authorize(Roles = "Provider")]
        public async Task<IActionResult> ProviderDashboard()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var acceptedCount = await _dbContext.AcceptedRequests.CountAsync(ar => ar.ProviderId == currentUser.Id);
            var completedCount = await _dbContext.AcceptedRequests.CountAsync(ar => ar.ProviderId == currentUser.Id && ar.Status == "Completed");
            var totalEarnings = await _dbContext.ServiceRequests.Where(r => r.AcceptedRequest != null && r.AcceptedRequest.ProviderId == currentUser.Id && r.Status == "Completed" && r.PaymentStatus == "Paid" && r.PaymentAmount.HasValue).SumAsync(r => r.PaymentAmount.Value);
            var model = new ProviderDashboardViewModel
            {
                AcceptedCount = acceptedCount,
                CompletedCount = completedCount,
                TotalEarnings = totalEarnings
            };
            return View(model);
        }

        // GET: ServiceRequest/RequesterDashboard
        [Authorize(Roles = "Requester")]
        public async Task<IActionResult> RequesterDashboard()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var createdCount = await _dbContext.ServiceRequests.CountAsync(r => r.RequesterId == currentUser.Id);
            var completedCount = await _dbContext.ServiceRequests.CountAsync(r => r.RequesterId == currentUser.Id && r.Status == "Completed");
            var totalSpent = await _dbContext.ServiceRequests.Where(r => r.RequesterId == currentUser.Id && r.Status == "Completed" && r.PaymentStatus == "Paid" && r.PaymentAmount.HasValue).SumAsync(r => r.PaymentAmount.Value);
            var model = new RequesterDashboardViewModel
            {
                CreatedCount = createdCount,
                CompletedCount = completedCount,
                TotalSpent = totalSpent
            };
            return View(model);
        }
    }
}