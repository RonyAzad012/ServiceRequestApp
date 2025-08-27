using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ServiceRequestApp.Data;
using ServiceRequestApp.Models;
using ServiceRequestApp.ViewModels;
using System.Linq;
using System.Threading.Tasks;

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

        public IActionResult Dashboard()
        {
            var model = new AdminDashboardViewModel
            {
                UserCount = _userManager.Users.Count(),
                ServiceRequestCount = _dbContext.ServiceRequests.Count()
            };
            return View(model);
        }

        public IActionResult Users()
        {
            var users = _userManager.Users.ToList();
            return View(users);
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

        public async Task<IActionResult> DeleteUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();
            return View(user);
        }

        [HttpPost, ActionName("DeleteUser")]
        public async Task<IActionResult> DeleteUserConfirmed(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            // Delete all service requests where this user is the requester
            var requests = _dbContext.ServiceRequests.Where(r => r.RequesterId == user.Id).ToList();
            if (requests.Any())
            {
                _dbContext.ServiceRequests.RemoveRange(requests);
                await _dbContext.SaveChangesAsync();
            }

            await _userManager.DeleteAsync(user);
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
