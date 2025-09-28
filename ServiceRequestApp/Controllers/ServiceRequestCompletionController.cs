using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ServiceRequestApp.Models;
using ServiceRequestApp.Services;

namespace ServiceRequestApp.Controllers
{
    [Authorize]
    public class ServiceRequestCompletionController : Controller
    {
        private readonly IServiceRequestCompletionService _completionService;
        private readonly UserManager<ApplicationUser> _userManager;

        public ServiceRequestCompletionController(
            IServiceRequestCompletionService completionService,
            UserManager<ApplicationUser> userManager)
        {
            _completionService = completionService;
            _userManager = userManager;
        }

        // POST: ServiceRequestCompletion/MarkInProgress
        [HttpPost]
        public async Task<IActionResult> MarkInProgress(int serviceRequestId)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var result = await _completionService.MarkAsInProgressAsync(serviceRequestId, currentUser.Id);

            if (result.Success)
            {
                TempData["SuccessMessage"] = result.Message;
            }
            else
            {
                TempData["ErrorMessage"] = result.Message;
            }

            return RedirectToAction("Details", "ServiceRequest", new { id = serviceRequestId });
        }

        // POST: ServiceRequestCompletion/RequestCompletion
        [HttpPost]
        public async Task<IActionResult> RequestCompletion(int serviceRequestId)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var result = await _completionService.RequestCompletionAsync(serviceRequestId, currentUser.Id);

            if (result.Success)
            {
                TempData["SuccessMessage"] = result.Message;
            }
            else
            {
                TempData["ErrorMessage"] = result.Message;
            }

            return RedirectToAction("Details", "ServiceRequest", new { id = serviceRequestId });
        }

        // POST: ServiceRequestCompletion/ApproveCompletion
        [HttpPost]
        public async Task<IActionResult> ApproveCompletion(int serviceRequestId)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var result = await _completionService.ApproveCompletionAsync(serviceRequestId, currentUser.Id);

            if (result.Success)
            {
                TempData["SuccessMessage"] = result.Message;
            }
            else
            {
                TempData["ErrorMessage"] = result.Message;
            }

            return RedirectToAction("Details", "ServiceRequest", new { id = serviceRequestId });
        }

        // POST: ServiceRequestCompletion/RejectCompletion
        [HttpPost]
        public async Task<IActionResult> RejectCompletion(int serviceRequestId, string reason)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var result = await _completionService.RejectCompletionAsync(serviceRequestId, currentUser.Id, reason);

            if (result.Success)
            {
                TempData["SuccessMessage"] = result.Message;
            }
            else
            {
                TempData["ErrorMessage"] = result.Message;
            }

            return RedirectToAction("Details", "ServiceRequest", new { id = serviceRequestId });
        }

        // POST: ServiceRequestCompletion/CancelRequest
        [HttpPost]
        public async Task<IActionResult> CancelRequest(int serviceRequestId, string reason)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var result = await _completionService.MarkAsCancelledAsync(serviceRequestId, currentUser.Id, reason);

            if (result.Success)
            {
                TempData["SuccessMessage"] = result.Message;
            }
            else
            {
                TempData["ErrorMessage"] = result.Message;
            }

            return RedirectToAction("Details", "ServiceRequest", new { id = serviceRequestId });
        }

        // GET: ServiceRequestCompletion/PendingCompletions
        public async Task<IActionResult> PendingCompletions()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var pendingCompletions = await _completionService.GetPendingCompletionsAsync(currentUser.Id);

            return View(pendingCompletions);
        }

        // API: Get completion status
        [HttpGet]
        public async Task<IActionResult> GetCompletionStatus(int serviceRequestId)
        {
            var status = await _completionService.GetCompletionStatusAsync(serviceRequestId);
            return Json(status);
        }

        // API: Mark as completed (direct completion without approval)
        [HttpPost]
        public async Task<IActionResult> MarkCompleted(int serviceRequestId)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            
            // Determine completion type based on user role
            var user = await _userManager.GetUserAsync(User);
            string completionType = user.UserType == "Provider" || user.UserType == "Tasker" ? "Provider" : "Requester";
            
            var result = await _completionService.MarkAsCompletedAsync(serviceRequestId, currentUser.Id, completionType);

            return Json(new { 
                success = result.Success, 
                message = result.Message,
                newStatus = result.NewStatus
            });
        }
    }
}

