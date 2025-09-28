using ServiceRequestApp.Data;
using ServiceRequestApp.Models;
using Microsoft.EntityFrameworkCore;

namespace ServiceRequestApp.Services
{
    public interface IReviewService
    {
        Task<ReviewResult> CreateReviewAsync(CreateReviewRequest request);
        Task<List<Review>> GetUserReviewsAsync(string userId, int page = 1, int pageSize = 10);
        Task<List<Review>> GetProviderReviewsAsync(string providerId, int page = 1, int pageSize = 10);
        Task<ReviewSummary> GetReviewSummaryAsync(string userId);
        Task<bool> CanUserReviewAsync(string userId, int serviceRequestId);
        Task<Review> GetReviewByServiceRequestAsync(int serviceRequestId);
        Task<ReviewResult> UpdateReviewAsync(int reviewId, UpdateReviewRequest request);
        Task<bool> DeleteReviewAsync(int reviewId, string userId);
    }

    public class ReviewService : IReviewService
    {
        private readonly ApplicationDbContext _dbContext;

        public ReviewService(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<ReviewResult> CreateReviewAsync(CreateReviewRequest request)
        {
            try
            {
                // Check if user can review
                if (!await CanUserReviewAsync(request.ReviewerId, request.ServiceRequestId))
                {
                    return new ReviewResult
                    {
                        Success = false,
                        Message = "You cannot review this service request"
                    };
                }

                // Check if review already exists
                var existingReview = await _dbContext.Reviews
                    .FirstOrDefaultAsync(r => r.ServiceRequestId == request.ServiceRequestId);

                if (existingReview != null)
                {
                    return new ReviewResult
                    {
                        Success = false,
                        Message = "You have already reviewed this service"
                    };
                }

                var review = new Review
                {
                    ServiceRequestId = request.ServiceRequestId,
                    ReviewerId = request.ReviewerId,
                    RevieweeId = request.RevieweeId,
                    Rating = request.Rating,
                    Comment = request.Comment,
                    CreatedAt = DateTime.UtcNow
                };

                _dbContext.Reviews.Add(review);

                // Update provider's average rating
                await UpdateProviderRatingAsync(request.RevieweeId);

                await _dbContext.SaveChangesAsync();

                return new ReviewResult
                {
                    Success = true,
                    Message = "Review submitted successfully",
                    Review = review
                };
            }
            catch (Exception ex)
            {
                return new ReviewResult
                {
                    Success = false,
                    Message = $"Error creating review: {ex.Message}"
                };
            }
        }

        public async Task<List<Review>> GetUserReviewsAsync(string userId, int page = 1, int pageSize = 10)
        {
            return await _dbContext.Reviews
                .Where(r => r.RevieweeId == userId)
                .Include(r => r.Reviewer)
                .Include(r => r.ServiceRequest)
                .OrderByDescending(r => r.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<List<Review>> GetProviderReviewsAsync(string providerId, int page = 1, int pageSize = 10)
        {
            return await _dbContext.Reviews
                .Where(r => r.RevieweeId == providerId)
                .Include(r => r.Reviewer)
                .Include(r => r.ServiceRequest)
                .OrderByDescending(r => r.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<ReviewSummary> GetReviewSummaryAsync(string userId)
        {
            var reviews = await _dbContext.Reviews
                .Where(r => r.RevieweeId == userId)
                .ToListAsync();

            if (!reviews.Any())
            {
                return new ReviewSummary
                {
                    UserId = userId,
                    AverageRating = 0,
                    TotalReviews = 0,
                    RatingDistribution = new Dictionary<int, int>()
                };
            }

            var averageRating = reviews.Average(r => r.Rating);
            var totalReviews = reviews.Count;
            var ratingDistribution = reviews
                .GroupBy(r => r.Rating)
                .ToDictionary(g => g.Key, g => g.Count());

            return new ReviewSummary
            {
                UserId = userId,
                AverageRating = Math.Round(averageRating, 1),
                TotalReviews = totalReviews,
                RatingDistribution = ratingDistribution
            };
        }

        public async Task<bool> CanUserReviewAsync(string userId, int serviceRequestId)
        {
            var serviceRequest = await _dbContext.ServiceRequests
                .Include(sr => sr.AcceptedRequest)
                .FirstOrDefaultAsync(sr => sr.Id == serviceRequestId);

            if (serviceRequest == null)
                return false;

            // Check if service request is completed
            if (serviceRequest.Status != "Completed")
                return false;

            // Check if user is the requester
            if (serviceRequest.RequesterId == userId)
                return true;

            // Check if user is the provider
            if (serviceRequest.AcceptedRequest?.ProviderId == userId)
                return true;

            return false;
        }

        public async Task<Review> GetReviewByServiceRequestAsync(int serviceRequestId)
        {
            return await _dbContext.Reviews
                .Include(r => r.Reviewer)
                .Include(r => r.Reviewee)
                .FirstOrDefaultAsync(r => r.ServiceRequestId == serviceRequestId);
        }

        public async Task<ReviewResult> UpdateReviewAsync(int reviewId, UpdateReviewRequest request)
        {
            try
            {
                var review = await _dbContext.Reviews.FindAsync(reviewId);
                if (review == null)
                {
                    return new ReviewResult
                    {
                        Success = false,
                        Message = "Review not found"
                    };
                }

                if (review.ReviewerId != request.ReviewerId)
                {
                    return new ReviewResult
                    {
                        Success = false,
                        Message = "Unauthorized to update this review"
                    };
                }

                review.Rating = request.Rating;
                review.Comment = request.Comment;
                review.UpdatedAt = DateTime.UtcNow;

                // Update provider's average rating
                await UpdateProviderRatingAsync(review.RevieweeId);

                await _dbContext.SaveChangesAsync();

                return new ReviewResult
                {
                    Success = true,
                    Message = "Review updated successfully",
                    Review = review
                };
            }
            catch (Exception ex)
            {
                return new ReviewResult
                {
                    Success = false,
                    Message = $"Error updating review: {ex.Message}"
                };
            }
        }

        public async Task<bool> DeleteReviewAsync(int reviewId, string userId)
        {
            try
            {
                var review = await _dbContext.Reviews.FindAsync(reviewId);
                if (review == null)
                    return false;

                if (review.ReviewerId != userId)
                    return false;

                _dbContext.Reviews.Remove(review);

                // Update provider's average rating
                await UpdateProviderRatingAsync(review.RevieweeId);

                await _dbContext.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        private async Task UpdateProviderRatingAsync(string providerId)
        {
            var reviews = await _dbContext.Reviews
                .Where(r => r.RevieweeId == providerId)
                .ToListAsync();

            var user = await _dbContext.Users.FindAsync(providerId);
            if (user != null)
            {
                if (reviews.Any())
                {
                    user.AverageRating = Math.Round(reviews.Average(r => r.Rating), 1);
                    user.TotalReviews = reviews.Count;
                }
                else
                {
                    user.AverageRating = null;
                    user.TotalReviews = 0;
                }
            }
        }
    }

    // Request and Result Models
    public class CreateReviewRequest
    {
        public int ServiceRequestId { get; set; }
        public string ReviewerId { get; set; }
        public string RevieweeId { get; set; }
        public int Rating { get; set; }
        public string Comment { get; set; }
    }

    public class UpdateReviewRequest
    {
        public string ReviewerId { get; set; }
        public int Rating { get; set; }
        public string Comment { get; set; }
    }

    public class ReviewResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public Review? Review { get; set; }
    }

    public class ReviewSummary
    {
        public string UserId { get; set; }
        public double AverageRating { get; set; }
        public int TotalReviews { get; set; }
        public Dictionary<int, int> RatingDistribution { get; set; } = new();
    }
}

