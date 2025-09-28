using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ServiceRequestApp.Models;
using ServiceRequestApp.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace ServiceRequestApp.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _dbContext;
        public HomeController(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public IActionResult Index()
        {
            // Pass categories to the view for the search bar
            var categories = _dbContext.Categories.ToList();
            ViewBag.Categories = categories;

            // Pass current user and admin flag to the view
            bool isAdmin = false;
            if (User.Identity.IsAuthenticated)
            {
                var userManager = HttpContext.RequestServices.GetService(typeof(UserManager<ApplicationUser>)) as UserManager<ApplicationUser>;
                var currentUser = userManager.GetUserAsync(User).Result;
                ViewBag.CurrentUser = currentUser;
                if (currentUser != null)
                {
                    var roles = userManager.GetRolesAsync(currentUser).Result;
                    isAdmin = roles.Contains("Admin");
                }
            }
            ViewBag.IsAdmin = isAdmin;
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Search(string category, string location)
        {
            // Find providers matching category and location
            var providers = _dbContext.Users
                .Where(u => (u.UserType == "Provider" || u.UserType == "Business" || u.UserType == "Tasker") 
                           && u.IsApproved && u.IsAvailable)
                .Include(u => u.PrimaryCategory)
                .AsQueryable();
                
            if (!string.IsNullOrEmpty(category))
            {
                // Search by category name or provider's primary category
                providers = providers.Where(u => 
                    u.PrimaryCategory != null && u.PrimaryCategory.Name.Contains(category) ||
                    u.BusinessCredentials.Contains(category) || 
                    u.ShopName.Contains(category) ||
                    u.Skills.Contains(category));
            }
            
            if (!string.IsNullOrEmpty(location))
            {
                providers = providers.Where(u => 
                    u.Address.Contains(location) || 
                    u.ShopAddress.Contains(location) || 
                    u.Zipcode.Contains(location) ||
                    u.ServiceAreas.Contains(location));
            }
            
            var results = await providers
                .OrderByDescending(u => u.AverageRating)
                .ThenByDescending(u => u.TotalReviews)
                .ToListAsync();
                
            ViewBag.Category = category;
            ViewBag.Location = location;
            return View("SearchResults", results);
        }

        [HttpGet]
        public async Task<IActionResult> GetFeaturedProviders()
        {
            var providers = await _dbContext.Users
                .Where(u => (u.UserType == "Provider" || u.UserType == "Business" || u.UserType == "Tasker") 
                           && u.IsApproved && u.IsAvailable)
                .Include(u => u.PrimaryCategory)
                .OrderByDescending(u => u.AverageRating)
                .ThenByDescending(u => u.TotalReviews)
                .Take(6)
                .Select(u => new
                {
                    u.Id,
                    u.FirstName,
                    u.LastName,
                    u.ShopName,
                    u.ShopDescription,
                    u.ProfileDescription,
                    u.BusinessImagePath,
                    u.ProfileImagePath,
                    u.AverageRating,
                    u.TotalReviews,
                    u.Address,
                    u.ShopAddress,
                    CategoryName = u.PrimaryCategory != null ? u.PrimaryCategory.Name : "General",
                    CategoryIcon = u.PrimaryCategory != null ? u.PrimaryCategory.Icon : "fas fa-tag",
                    CategoryColor = u.PrimaryCategory != null ? u.PrimaryCategory.Color : "#007bff"
                })
                .ToListAsync();

            return Json(new { success = true, data = providers });
        }
    }
}