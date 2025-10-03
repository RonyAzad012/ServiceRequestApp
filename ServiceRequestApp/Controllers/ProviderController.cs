using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ServiceRequestApp.Data;
using ServiceRequestApp.Models;
using System.Linq;
using System.Threading.Tasks;

namespace ServiceRequestApp.Controllers
{
    public class ProviderController : Controller
    {
        private readonly ApplicationDbContext _dbContext;

        public ProviderController(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<IActionResult> Index(int? categoryId, string location = "")
        {
            var query = _dbContext.Users
                .Where(u => (u.UserType == "Provider" || u.UserType == "Business" || u.UserType == "Tasker") 
                           && u.IsApproved)
                .Include(u => u.PrimaryCategory)
                .AsQueryable();

            // Filter by category
            if (categoryId.HasValue)
            {
                query = query.Where(u => u.PrimaryCategoryId == categoryId.Value);
            }

            // Filter by location - improved search
            if (!string.IsNullOrEmpty(location))
            {
                query = query.Where(u => 
                    (u.City != null && u.City.Contains(location)) ||
                    (u.Address != null && u.Address.Contains(location)) || 
                    (u.ShopAddress != null && u.ShopAddress.Contains(location)) || 
                    (u.Zipcode != null && u.Zipcode.Contains(location)) ||
                    (u.ServiceAreas != null && u.ServiceAreas.Contains(location)));
            }

            var providers = await query
                .OrderByDescending(u => u.AverageRating)
                .ThenByDescending(u => u.TotalReviews)
                .ToListAsync();

            // Get categories for filter dropdown - only show categories that have providers
            var categories = await _dbContext.Categories
                .Where(c => c.IsActive && _dbContext.Users.Any(u => (u.UserType == "Provider" || u.UserType == "Business" || u.UserType == "Tasker") && u.PrimaryCategoryId == c.Id))
                .OrderBy(c => c.Name)
                .ToListAsync();

            ViewBag.Categories = categories;
            ViewBag.SelectedCategoryId = categoryId;
            ViewBag.Location = location;

            return View(providers);
        }

        [HttpGet]
        public async Task<IActionResult> GetProviders(int? categoryId, string location = "")
        {
            var query = _dbContext.Users
                .Where(u => (u.UserType == "Provider" || u.UserType == "Business" || u.UserType == "Tasker") 
                           && u.IsApproved)
                .Include(u => u.PrimaryCategory)
                .AsQueryable();

            if (categoryId.HasValue)
            {
                query = query.Where(u => u.PrimaryCategoryId == categoryId.Value);
            }

            if (!string.IsNullOrEmpty(location))
            {
                query = query.Where(u => 
                    (u.City != null && u.City.Contains(location)) ||
                    (u.Address != null && u.Address.Contains(location)) || 
                    (u.ShopAddress != null && u.ShopAddress.Contains(location)) || 
                    (u.Zipcode != null && u.Zipcode.Contains(location)) ||
                    (u.ServiceAreas != null && u.ServiceAreas.Contains(location)));
            }

            var providers = await query
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
                    u.PhoneNumber,
                    u.ShopPhone,
                    u.Latitude,
                    u.Longitude,
                    u.UserType,
                    CategoryName = u.PrimaryCategory != null ? u.PrimaryCategory.Name : "General",
                    CategoryIcon = u.PrimaryCategory != null ? u.PrimaryCategory.Icon : "fas fa-tag",
                    CategoryColor = u.PrimaryCategory != null ? u.PrimaryCategory.Color : "#007bff"
                })
                .OrderByDescending(u => u.AverageRating)
                .ThenByDescending(u => u.TotalReviews)
                .ToListAsync();

            return Json(new { success = true, data = providers });
        }

        public async Task<IActionResult> Details(string id)
        {
            var provider = await _dbContext.Users
                .Include(u => u.PrimaryCategory)
                .Include(u => u.Reviews)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (provider == null || !provider.IsApproved)
            {
                return NotFound();
            }

            // Get recent reviews
            var reviews = await _dbContext.Reviews
                .Where(r => r.RevieweeId == id)
                .Include(r => r.Reviewer)
                .OrderByDescending(r => r.CreatedAt)
                .Take(10)
                .ToListAsync();

            ViewBag.Reviews = reviews;

            return View(provider);
        }

        [HttpGet]
        public async Task<IActionResult> GetCategories()
        {
            var categories = await _dbContext.Categories
                .Where(c => c.IsActive)
                .Select(c => new { c.Id, c.Name, c.Icon, c.Color })
                .OrderBy(c => c.Name)
                .ToListAsync();

            return Json(categories);
        }
    }
}

