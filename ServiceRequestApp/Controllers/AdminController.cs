using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ServiceRequestApp.Data;
using ServiceRequestApp.Models;
using ServiceRequestApp.ViewModels;
using System.Linq;
using System.Threading.Tasks;
using System;
using Microsoft.EntityFrameworkCore;

namespace ServiceRequestApp.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly UserManager<ApplicationUser> _userManager;

        public AdminController(ApplicationDbContext dbContext, UserManager<ApplicationUser> userManager)
        {
            _dbContext = dbContext;
            _userManager = userManager;
        }

        public async Task<IActionResult> Dashboard()
        {
            try
            {
                var model = new AdminDashboardViewModel
                {
                    UserCount = await _userManager.Users.CountAsync(),
                    ServiceRequestCount = await _dbContext.ServiceRequests.CountAsync(),
                    CompletedRequestCount = await _dbContext.ServiceRequests.CountAsync(r => r.Status == "Completed"),
                    PaidRequestCount = await _dbContext.ServiceRequests.CountAsync(r => r.PaymentStatus == "Paid"),
                    TotalPayments = await _dbContext.ServiceRequests
                        .Where(r => r.PaymentStatus == "Paid" && r.PaymentAmount.HasValue)
                        .SumAsync(r => r.PaymentAmount.Value),
                    ProviderCount = await _userManager.Users.CountAsync(u => u.UserType == "Provider"),
                    RequesterCount = await _userManager.Users.CountAsync(u => u.UserType == "Requester")
                };
                return View(model);
            }
            catch (Exception ex)
            {
                // Log the exception
                return View(new AdminDashboardViewModel());
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetDashboardStats()
        {
            try
            {
                var stats = new
                {
                    UserCount = await _userManager.Users.CountAsync(),
                    ServiceRequestCount = await _dbContext.ServiceRequests.CountAsync(),
                    CompletedRequestCount = await _dbContext.ServiceRequests.CountAsync(r => r.Status == "Completed"),
                    PaidRequestCount = await _dbContext.ServiceRequests.CountAsync(r => r.PaymentStatus == "Paid"),
                    TotalPayments = await _dbContext.ServiceRequests
                        .Where(r => r.PaymentStatus == "Paid" && r.PaymentAmount.HasValue)
                        .SumAsync(r => r.PaymentAmount.Value),
                    ProviderCount = await _userManager.Users.CountAsync(u => u.UserType == "Provider"),
                    RequesterCount = await _userManager.Users.CountAsync(u => u.UserType == "Requester")
                };
                return Json(new { success = true, data = stats });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error fetching dashboard statistics" });
            }
        }

        public async Task<IActionResult> Users()
        {
            try
            {
                var users = await _userManager.Users
                    .OrderByDescending(u => u.CreatedAt)
                    .ToListAsync();
                return View(users);
            }
            catch (Exception ex)
            {
                return View(new List<ApplicationUser>());
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetUsers()
        {
            try
            {
                var users = await _userManager.Users
                    .Select(u => new
                    {
                        u.Id,
                        u.FirstName,
                        u.LastName,
                        u.Email,
                        u.UserType,
                        u.PhoneNumber,
                        u.CreatedAt,
                        IsActive = u.EmailConfirmed
                    })
                    .OrderByDescending(u => u.CreatedAt)
                    .ToListAsync();
                
                return Json(new { success = true, data = users });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error fetching users" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> ToggleUserStatus(string userId)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                    return Json(new { success = false, message = "User not found" });

                user.EmailConfirmed = !user.EmailConfirmed;
                var result = await _userManager.UpdateAsync(user);

                if (result.Succeeded)
                {
                    return Json(new { 
                        success = true, 
                        message = $"User {(user.EmailConfirmed ? "activated" : "deactivated")} successfully",
                        isActive = user.EmailConfirmed
                    });
                }
                else
                {
                    return Json(new { success = false, message = "Failed to update user status" });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error updating user status" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> ApproveUser(string userId)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                    return Json(new { success = false, message = "User not found" });

                user.IsApproved = true;
                user.ApprovedAt = DateTime.UtcNow;
                user.ApprovedBy = User.Identity.Name;
                user.RejectionReason = null; // Clear any previous rejection reason

                var result = await _userManager.UpdateAsync(user);

                if (result.Succeeded)
                {
                    return Json(new { 
                        success = true, 
                        message = "User approved successfully",
                        isApproved = user.IsApproved
                    });
                }
                else
                {
                    return Json(new { success = false, message = "Failed to approve user" });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error approving user" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> RejectUser(string userId, string reason)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                    return Json(new { success = false, message = "User not found" });

                user.IsApproved = false;
                user.RejectionReason = reason;
                user.ApprovedAt = null;
                user.ApprovedBy = null;

                var result = await _userManager.UpdateAsync(user);

                if (result.Succeeded)
                {
                    return Json(new { 
                        success = true, 
                        message = "User rejected successfully",
                        isApproved = user.IsApproved
                    });
                }
                else
                {
                    return Json(new { success = false, message = "Failed to reject user" });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error rejecting user" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetPendingApprovals()
        {
            try
            {
                var pendingUsers = await _userManager.Users
                    .Where(u => !u.IsApproved && (u.UserType == "Provider" || u.UserType == "Tasker" || u.UserType == "Business"))
                    .Include(u => u.PrimaryCategory)
                    .Select(u => new
                    {
                        u.Id,
                        u.FirstName,
                        u.LastName,
                        u.Email,
                        u.UserType,
                        u.PhoneNumber,
                        u.CreatedAt,
                        u.ShopName,
                        u.Skills,
                        u.BusinessCredentials,
                        u.NationalId,
                        CategoryName = u.PrimaryCategory != null ? u.PrimaryCategory.Name : "Not Selected",
                        u.RejectionReason
                    })
                    .OrderByDescending(u => u.CreatedAt)
                    .ToListAsync();

                return Json(new { success = true, data = pendingUsers });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error fetching pending approvals" });
            }
        }

        [HttpDelete]
        public async Task<IActionResult> DeleteUser(string userId)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                    return Json(new { success = false, message = "User not found" });

                // Check if user has any service requests
                var hasRequests = await _dbContext.ServiceRequests.AnyAsync(r => r.RequesterId == userId);
                if (hasRequests)
                {
                    return Json(new { success = false, message = "Cannot delete user with existing service requests" });
                }

                var result = await _userManager.DeleteAsync(user);
                if (result.Succeeded)
                {
                    return Json(new { success = true, message = "User deleted successfully" });
                }
                else
                {
                    return Json(new { success = false, message = "Failed to delete user" });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error deleting user" });
            }
        }

        public IActionResult EditUser(string id)
        {
            var user = _userManager.Users.FirstOrDefault(u => u.Id == id);
            if (user == null) return NotFound();
            return View(user);
        }

        [HttpPost]
        public async Task<IActionResult> EditUser(ApplicationUser model)
        {
            var user = await _userManager.FindByIdAsync(model.Id);
            if (user == null) return NotFound();
            user.FirstName = model.FirstName;
            user.LastName = model.LastName;
            user.Email = model.Email;
            user.UserName = model.Email;
            user.Address = model.Address;
            user.UserType = model.UserType;
            await _userManager.UpdateAsync(user);
            return RedirectToAction("Users");
        }


        public IActionResult ServiceRequests()
        {
            var requests = _dbContext.ServiceRequests.ToList();
            return View(requests);
        }

        public IActionResult EditRequest(int id)
        {
            var req = _dbContext.ServiceRequests.FirstOrDefault(r => r.Id == id);
            if (req == null) return NotFound();
            return View(req);
        }

        [HttpPost]
        public IActionResult EditRequest(ServiceRequest model)
        {
            var req = _dbContext.ServiceRequests.FirstOrDefault(r => r.Id == model.Id);
            if (req == null) return NotFound();
            req.Title = model.Title;
            req.Description = model.Description;
            req.ServiceType = model.ServiceType;
            req.Status = model.Status;
            _dbContext.SaveChanges();
            return RedirectToAction("ServiceRequests");
        }

        public IActionResult DeleteRequest(int id)
        {
            var req = _dbContext.ServiceRequests.FirstOrDefault(r => r.Id == id);
            if (req == null) return NotFound();
            return View(req);
        }

        [HttpPost, ActionName("DeleteRequest")]
        public IActionResult DeleteRequestConfirmed(int id)
        {
            var req = _dbContext.ServiceRequests.FirstOrDefault(r => r.Id == id);
            if (req == null) return NotFound();
            _dbContext.ServiceRequests.Remove(req);
            _dbContext.SaveChanges();
            return RedirectToAction("ServiceRequests");
        }
    }
}
