using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ServiceRequestApp.Data;
using ServiceRequestApp.Models;
using ServiceRequestApp.ViewModels;

namespace ServiceRequestApp.Controllers
{
    [Authorize]
    public class ServiceRequestController : Controller
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly UserManager<ApplicationUser> _userManager;

        public ServiceRequestController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _dbContext = context;
            _userManager = userManager;
        }

        // GET: ServiceRequest
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
        [Authorize(Roles = "Requester")]
        public IActionResult Create()
        {
            return View();
        }

        // POST: ServiceRequest/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Requester")]
        public async Task<IActionResult> Create(CreateServiceRequestViewModel model)
        {
            if (ModelState.IsValid)
            {
                var currentUser = await _userManager.GetUserAsync(User);
                var request = new ServiceRequest
                {
                    Title = model.Title,
                    Description = model.Description,
                    ServiceType = model.ServiceType,
                    CreatedAt = DateTime.UtcNow,
                    Status = "Pending",
                    Latitude = model.Latitude,
                    Longitude = model.Longitude,
                    RequesterId = currentUser.Id
                };

                _dbContext.Add(request);
                await _dbContext.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(model);
        }

        // POST: ServiceRequest/Accept/5
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

            if (request.AcceptedRequest?.ProviderId != currentUser.Id &&
                request.RequesterId != currentUser.Id)
            {
                return Unauthorized();
            }

            request.Status = "Completed";
            if (request.AcceptedRequest != null)
            {
                request.AcceptedRequest.Status = "Completed";
            }

            await _dbContext.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET: ServiceRequest/Edit/5
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

            return View(request);
        }

        // POST: ServiceRequest/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Requester")]
        public async Task<IActionResult> Edit(int id, ServiceRequest request)
        {
            if (id != request.Id)
            {
                return NotFound();
            }

            var currentUser = await _userManager.GetUserAsync(User);
            var existingRequest = await _dbContext.ServiceRequests.FindAsync(id);

            if (existingRequest == null)
            {
                return NotFound();
            }

            // Only allow editing if the request belongs to the current user and is still pending
            if (existingRequest.RequesterId != currentUser.Id || existingRequest.Status != "Pending")
            {
                return Unauthorized();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Update only allowed fields
                    existingRequest.Title = request.Title;
                    existingRequest.Description = request.Description;
                    existingRequest.ServiceType = request.ServiceType;
                    existingRequest.Latitude = request.Latitude;
                    existingRequest.Longitude = request.Longitude;

                    await _dbContext.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ServiceRequestExists(request.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(request);
        }

        // GET: ServiceRequest/Delete/5
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

        private bool ServiceRequestExists(int id)
        {
            return _dbContext.ServiceRequests.Any(e => e.Id == id);
        }
    }
}