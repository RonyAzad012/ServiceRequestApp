using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ServiceRequestApp.Data;
using ServiceRequestApp.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace ServiceRequestApp.Controllers
{
    [Authorize]
    public class ReviewController : Controller
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly UserManager<ApplicationUser> _userManager;

        public ReviewController(ApplicationDbContext dbContext, UserManager<ApplicationUser> userManager)
        {
            _dbContext = dbContext;
            _userManager = userManager;
        }

        [HttpPost]
        public async Task<IActionResult> SubmitReview(int serviceRequestId, string providerId, int rating, string comment = null)
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null)
                {
                    return Json(new { success = false, message = "User not found" });
                }

                // Check if the service request exists and is completed
                var serviceRequest = await _dbContext.ServiceRequests
                    .Include(sr => sr.AcceptedRequest)
                    .FirstOrDefaultAsync(sr => sr.Id == serviceRequestId);

                if (serviceRequest == null)
                {
                    return Json(new { success = false, message = "Service request not found" });
                }

                if (serviceRequest.Status != "Completed")
                {
                    return Json(new { success = false, message = "Can only review completed services" });
                }

                // Check if the current user is the requester
                if (serviceRequest.RequesterId != currentUser.Id)
                {
                    return Json(new { success = false, message = "Only the requester can review this service" });
                }

                // Check if the provider is correct
                if (serviceRequest.AcceptedRequest?.ProviderId != providerId)
                {
                    return Json(new { success = false, message = "Invalid provider for this service request" });
                }

                // Check if review already exists
                var existingReview = await _dbContext.Reviews
                    .FirstOrDefaultAsync(r => r.ServiceRequestId == serviceRequestId && r.ReviewerId == currentUser.Id);

                if (existingReview != null)
                {
                    return Json(new { success = false, message = "You have already reviewed this service" });
                }

                // Create new review
                var review = new Review
                {
                    ServiceRequestId = serviceRequestId,
                    ReviewerId = currentUser.Id,
                    RevieweeId = providerId,
                    Rating = rating,
                    Comment = comment,
                    CreatedAt = DateTime.UtcNow
                };

                _dbContext.Reviews.Add(review);
                await _dbContext.SaveChangesAsync();

                return Json(new { success = true, message = "Review submitted successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "An error occurred while submitting the review" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetReviews(string providerId)
        {
            try
            {
                var reviews = await _dbContext.Reviews
                    .Where(r => r.RevieweeId == providerId)
                    .Include(r => r.Reviewer)
                    .Include(r => r.ServiceRequest)
                    .OrderByDescending(r => r.CreatedAt)
                    .ToListAsync();

                var reviewData = reviews.Select(r => new
                {
                    id = r.Id,
                    rating = r.Rating,
                    comment = r.Comment,
                    createdAt = r.CreatedAt,
                    reviewerName = $"{r.Reviewer.FirstName} {r.Reviewer.LastName}",
                    serviceTitle = r.ServiceRequest.Title
                }).ToList();

                return Json(new { success = true, reviews = reviewData });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "An error occurred while loading reviews" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAverageRating(string providerId)
        {
            try
            {
                var averageRating = await _dbContext.Reviews
                    .Where(r => r.RevieweeId == providerId)
                    .AverageAsync(r => (double?)r.Rating) ?? 0.0;

                var totalReviews = await _dbContext.Reviews
                    .CountAsync(r => r.RevieweeId == providerId);

                return Json(new { success = true, averageRating = Math.Round(averageRating, 1), totalReviews });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "An error occurred while loading rating" });
            }
        }
    }
}


