using ServiceRequestApp.Data;
using ServiceRequestApp.Models;
using Microsoft.EntityFrameworkCore;

namespace ServiceRequestApp.Services
{
    public interface IServiceRequestCompletionService
    {
        Task<CompletionResult> MarkAsInProgressAsync(int serviceRequestId, string userId);
        Task<CompletionResult> MarkAsCompletedAsync(int serviceRequestId, string userId, string completionType);
        Task<CompletionResult> MarkAsCancelledAsync(int serviceRequestId, string userId, string reason);
        Task<CompletionResult> RequestCompletionAsync(int serviceRequestId, string userId);
        Task<CompletionResult> ApproveCompletionAsync(int serviceRequestId, string approverId);
        Task<CompletionResult> RejectCompletionAsync(int serviceRequestId, string rejectorId, string reason);
        Task<List<ServiceRequest>> GetPendingCompletionsAsync(string userId);
        Task<CompletionStatus> GetCompletionStatusAsync(int serviceRequestId);
    }

    public class ServiceRequestCompletionService : IServiceRequestCompletionService
    {
        private readonly ApplicationDbContext _dbContext;

        public ServiceRequestCompletionService(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<CompletionResult> MarkAsInProgressAsync(int serviceRequestId, string userId)
        {
            try
            {
                var serviceRequest = await _dbContext.ServiceRequests
                    .Include(sr => sr.AcceptedRequest)
                    .FirstOrDefaultAsync(sr => sr.Id == serviceRequestId);

                if (serviceRequest == null)
                {
                    return new CompletionResult { Success = false, Message = "Service request not found" };
                }

                // Check authorization
                if (serviceRequest.AcceptedRequest?.ProviderId != userId)
                {
                    return new CompletionResult { Success = false, Message = "Unauthorized to update this request" };
                }

                // Check if request can be marked as in progress
                if (serviceRequest.Status != "Accepted")
                {
                    return new CompletionResult { Success = false, Message = "Request must be accepted before marking as in progress" };
                }

                serviceRequest.Status = "In Progress";
                serviceRequest.AcceptedRequest.Status = "In Progress";
                serviceRequest.UpdatedAt = DateTime.UtcNow;

                await _dbContext.SaveChangesAsync();

                return new CompletionResult 
                { 
                    Success = true, 
                    Message = "Service request marked as in progress",
                    NewStatus = "In Progress"
                };
            }
            catch (Exception ex)
            {
                return new CompletionResult { Success = false, Message = $"Error: {ex.Message}" };
            }
        }

        public async Task<CompletionResult> MarkAsCompletedAsync(int serviceRequestId, string userId, string completionType)
        {
            try
            {
                var serviceRequest = await _dbContext.ServiceRequests
                    .Include(sr => sr.AcceptedRequest)
                    .FirstOrDefaultAsync(sr => sr.Id == serviceRequestId);

                if (serviceRequest == null)
                {
                    return new CompletionResult { Success = false, Message = "Service request not found" };
                }

                // Check authorization
                bool isProvider = serviceRequest.AcceptedRequest?.ProviderId == userId;
                bool isRequester = serviceRequest.RequesterId == userId;

                if (!isProvider && !isRequester)
                {
                    return new CompletionResult { Success = false, Message = "Unauthorized to complete this request" };
                }

                // Check if request can be completed
                if (serviceRequest.Status != "In Progress")
                {
                    return new CompletionResult { Success = false, Message = "Request must be in progress before completion" };
                }

                // Check payment status for completion
                if (serviceRequest.PaymentStatus != "Paid")
                {
                    return new CompletionResult { Success = false, Message = "Payment must be completed before marking as completed" };
                }

                if (completionType == "Provider")
                {
                    serviceRequest.Status = "Completed";
                    serviceRequest.AcceptedRequest.Status = "Completed";
                    serviceRequest.CompletedAt = DateTime.UtcNow;
                }
                else if (completionType == "Requester")
                {
                    serviceRequest.Status = "Completed";
                    serviceRequest.AcceptedRequest.Status = "Completed";
                    serviceRequest.CompletedAt = DateTime.UtcNow;
                }

                serviceRequest.UpdatedAt = DateTime.UtcNow;

                await _dbContext.SaveChangesAsync();

                return new CompletionResult 
                { 
                    Success = true, 
                    Message = "Service request completed successfully",
                    NewStatus = "Completed"
                };
            }
            catch (Exception ex)
            {
                return new CompletionResult { Success = false, Message = $"Error: {ex.Message}" };
            }
        }

        public async Task<CompletionResult> MarkAsCancelledAsync(int serviceRequestId, string userId, string reason)
        {
            try
            {
                var serviceRequest = await _dbContext.ServiceRequests
                    .Include(sr => sr.AcceptedRequest)
                    .FirstOrDefaultAsync(sr => sr.Id == serviceRequestId);

                if (serviceRequest == null)
                {
                    return new CompletionResult { Success = false, Message = "Service request not found" };
                }

                // Check authorization
                bool isProvider = serviceRequest.AcceptedRequest?.ProviderId == userId;
                bool isRequester = serviceRequest.RequesterId == userId;

                if (!isProvider && !isRequester)
                {
                    return new CompletionResult { Success = false, Message = "Unauthorized to cancel this request" };
                }

                // Check if request can be cancelled
                if (serviceRequest.Status == "Completed" || serviceRequest.Status == "Cancelled")
                {
                    return new CompletionResult { Success = false, Message = "Request cannot be cancelled in current status" };
                }

                serviceRequest.Status = "Cancelled";
                serviceRequest.AcceptedRequest.Status = "Cancelled";
                serviceRequest.CancelledAt = DateTime.UtcNow;
                serviceRequest.CancellationReason = reason;
                serviceRequest.UpdatedAt = DateTime.UtcNow;

                await _dbContext.SaveChangesAsync();

                return new CompletionResult 
                { 
                    Success = true, 
                    Message = "Service request cancelled successfully",
                    NewStatus = "Cancelled"
                };
            }
            catch (Exception ex)
            {
                return new CompletionResult { Success = false, Message = $"Error: {ex.Message}" };
            }
        }

        public async Task<CompletionResult> RequestCompletionAsync(int serviceRequestId, string userId)
        {
            try
            {
                var serviceRequest = await _dbContext.ServiceRequests
                    .Include(sr => sr.AcceptedRequest)
                    .FirstOrDefaultAsync(sr => sr.Id == serviceRequestId);

                if (serviceRequest == null)
                {
                    return new CompletionResult { Success = false, Message = "Service request not found" };
                }

                // Check authorization
                bool isProvider = serviceRequest.AcceptedRequest?.ProviderId == userId;
                bool isRequester = serviceRequest.RequesterId == userId;

                if (!isProvider && !isRequester)
                {
                    return new CompletionResult { Success = false, Message = "Unauthorized to request completion" };
                }

                // Check if request can be completed
                if (serviceRequest.Status != "In Progress")
                {
                    return new CompletionResult { Success = false, Message = "Request must be in progress to request completion" };
                }

                // Check payment status
                if (serviceRequest.PaymentStatus != "Paid")
                {
                    return new CompletionResult { Success = false, Message = "Payment must be completed before requesting completion" };
                }

                // Set completion request status
                if (isProvider)
                {
                    serviceRequest.Status = "Completion Requested by Provider";
                    serviceRequest.AcceptedRequest.Status = "Completion Requested by Provider";
                }
                else if (isRequester)
                {
                    serviceRequest.Status = "Completion Requested by Requester";
                    serviceRequest.AcceptedRequest.Status = "Completion Requested by Requester";
                }

                serviceRequest.UpdatedAt = DateTime.UtcNow;

                await _dbContext.SaveChangesAsync();

                return new CompletionResult 
                { 
                    Success = true, 
                    Message = "Completion request submitted successfully",
                    NewStatus = serviceRequest.Status
                };
            }
            catch (Exception ex)
            {
                return new CompletionResult { Success = false, Message = $"Error: {ex.Message}" };
            }
        }

        public async Task<CompletionResult> ApproveCompletionAsync(int serviceRequestId, string approverId)
        {
            try
            {
                var serviceRequest = await _dbContext.ServiceRequests
                    .Include(sr => sr.AcceptedRequest)
                    .FirstOrDefaultAsync(sr => sr.Id == serviceRequestId);

                if (serviceRequest == null)
                {
                    return new CompletionResult { Success = false, Message = "Service request not found" };
                }

                // Check authorization
                bool isProvider = serviceRequest.AcceptedRequest?.ProviderId == approverId;
                bool isRequester = serviceRequest.RequesterId == approverId;

                if (!isProvider && !isRequester)
                {
                    return new CompletionResult { Success = false, Message = "Unauthorized to approve completion" };
                }

                // Check if completion can be approved
                if (!serviceRequest.Status.Contains("Completion Requested"))
                {
                    return new CompletionResult { Success = false, Message = "No completion request to approve" };
                }

                serviceRequest.Status = "Completed";
                serviceRequest.AcceptedRequest.Status = "Completed";
                serviceRequest.CompletedAt = DateTime.UtcNow;
                serviceRequest.UpdatedAt = DateTime.UtcNow;

                await _dbContext.SaveChangesAsync();

                return new CompletionResult 
                { 
                    Success = true, 
                    Message = "Completion approved successfully",
                    NewStatus = "Completed"
                };
            }
            catch (Exception ex)
            {
                return new CompletionResult { Success = false, Message = $"Error: {ex.Message}" };
            }
        }

        public async Task<CompletionResult> RejectCompletionAsync(int serviceRequestId, string rejectorId, string reason)
        {
            try
            {
                var serviceRequest = await _dbContext.ServiceRequests
                    .Include(sr => sr.AcceptedRequest)
                    .FirstOrDefaultAsync(sr => sr.Id == serviceRequestId);

                if (serviceRequest == null)
                {
                    return new CompletionResult { Success = false, Message = "Service request not found" };
                }

                // Check authorization
                bool isProvider = serviceRequest.AcceptedRequest?.ProviderId == rejectorId;
                bool isRequester = serviceRequest.RequesterId == rejectorId;

                if (!isProvider && !isRequester)
                {
                    return new CompletionResult { Success = false, Message = "Unauthorized to reject completion" };
                }

                // Check if completion can be rejected
                if (!serviceRequest.Status.Contains("Completion Requested"))
                {
                    return new CompletionResult { Success = false, Message = "No completion request to reject" };
                }

                serviceRequest.Status = "In Progress";
                serviceRequest.AcceptedRequest.Status = "In Progress";
                serviceRequest.CompletionRejectionReason = reason;
                serviceRequest.UpdatedAt = DateTime.UtcNow;

                await _dbContext.SaveChangesAsync();

                return new CompletionResult 
                { 
                    Success = true, 
                    Message = "Completion rejected successfully",
                    NewStatus = "In Progress"
                };
            }
            catch (Exception ex)
            {
                return new CompletionResult { Success = false, Message = $"Error: {ex.Message}" };
            }
        }

        public async Task<List<ServiceRequest>> GetPendingCompletionsAsync(string userId)
        {
            return await _dbContext.ServiceRequests
                .Include(sr => sr.Requester)
                .Include(sr => sr.AcceptedRequest)
                .ThenInclude(ar => ar.Provider)
                .Where(sr => (sr.RequesterId == userId || sr.AcceptedRequest.ProviderId == userId) &&
                           sr.Status.Contains("Completion Requested"))
                .OrderByDescending(sr => sr.UpdatedAt)
                .ToListAsync();
        }

        public async Task<CompletionStatus> GetCompletionStatusAsync(int serviceRequestId)
        {
            var serviceRequest = await _dbContext.ServiceRequests
                .Include(sr => sr.AcceptedRequest)
                .FirstOrDefaultAsync(sr => sr.Id == serviceRequestId);

            if (serviceRequest == null)
            {
                return new CompletionStatus { IsValid = false, Message = "Service request not found" };
            }

            return new CompletionStatus
            {
                IsValid = true,
                CurrentStatus = serviceRequest.Status,
                CanMarkInProgress = serviceRequest.Status == "Accepted",
                CanRequestCompletion = serviceRequest.Status == "In Progress" && serviceRequest.PaymentStatus == "Paid",
                CanApproveCompletion = serviceRequest.Status.Contains("Completion Requested"),
                CanCancel = !new[] { "Completed", "Cancelled" }.Contains(serviceRequest.Status),
                PaymentStatus = serviceRequest.PaymentStatus,
                HasPayment = serviceRequest.PaymentStatus == "Paid"
            };
        }
    }

    // Result and Status models
    public class CompletionResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string NewStatus { get; set; }
        public Dictionary<string, object> AdditionalData { get; set; } = new();
    }

    public class CompletionStatus
    {
        public bool IsValid { get; set; }
        public string Message { get; set; }
        public string CurrentStatus { get; set; }
        public bool CanMarkInProgress { get; set; }
        public bool CanRequestCompletion { get; set; }
        public bool CanApproveCompletion { get; set; }
        public bool CanCancel { get; set; }
        public string PaymentStatus { get; set; }
        public bool HasPayment { get; set; }
    }
}

