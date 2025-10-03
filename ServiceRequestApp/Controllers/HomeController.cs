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
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                var userManager = HttpContext.RequestServices.GetService(typeof(UserManager<ApplicationUser>)) as UserManager<ApplicationUser>;
                if (userManager != null)
                {
                    var currentUser = userManager.GetUserAsync(User).Result;
                    ViewBag.CurrentUser = currentUser;
                    if (currentUser != null)
                    {
                        var roles = userManager.GetRolesAsync(currentUser).Result;
                        isAdmin = roles.Contains("Admin");
                    }
                }
            }
            ViewBag.IsAdmin = isAdmin;
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Search(int? categoryId, string street, string city, string zipcode)
        {
            // Find providers matching category and location
            var providers = _dbContext.Users
                .Where(u => (u.UserType == "Provider" || u.UserType == "Business" || u.UserType == "Tasker") 
                           && u.IsApproved && u.IsAvailable)
                .Include(u => u.PrimaryCategory)
                .AsQueryable();
            
            // Category filter by exact id
            if (categoryId.HasValue && categoryId.Value > 0)
            {
                providers = providers.Where(u => u.PrimaryCategoryId == categoryId.Value);
            }

            // Street/City/Zip filters
            if (!string.IsNullOrWhiteSpace(street))
                providers = providers.Where(u => (u.Street != null && u.Street.Contains(street)) || (u.ShopAddress != null && u.ShopAddress.Contains(street)));
            if (!string.IsNullOrWhiteSpace(city))
                providers = providers.Where(u => (u.City != null && u.City.Contains(city)) || (u.Address != null && u.Address.Contains(city)) || (u.ShopAddress != null && u.ShopAddress.Contains(city)));
            if (!string.IsNullOrWhiteSpace(zipcode))
                providers = providers.Where(u => u.Zipcode != null && u.Zipcode.Contains(zipcode));
            
            var results = await providers
                .OrderByDescending(u => u.AverageRating)
                .ThenByDescending(u => u.TotalReviews)
                .ToListAsync();
                
            // If no results found, redirect to AllProviders page
            if (!results.Any())
            {
                // Build route values for AllProviders
                var routeValues = new Dictionary<string, object>();
                if (categoryId.HasValue && categoryId.Value > 0)
                {
                    var selectedCategory = await _dbContext.Categories.FirstOrDefaultAsync(c => c.Id == categoryId.Value);
                    if (selectedCategory != null)
                    {
                        routeValues["category"] = selectedCategory.Name;
                    }
                }
                if (!string.IsNullOrWhiteSpace(city))
                {
                    routeValues["city"] = city;
                }
                if (!string.IsNullOrWhiteSpace(street))
                {
                    routeValues["search"] = street;
                }
                
                return RedirectToAction("AllProviders", "Account", routeValues);
            }
                
            // For UI display
            ViewBag.Street = street;
            ViewBag.City = city;
            ViewBag.Zipcode = zipcode;
            ViewBag.SelectedCategoryId = categoryId;
            if (categoryId.HasValue && categoryId.Value > 0)
            {
                var selectedCategory = await _dbContext.Categories.FirstOrDefaultAsync(c => c.Id == categoryId.Value);
                ViewBag.CategoryName = selectedCategory?.Name;
            }
            return View("SearchResults", results);
        }

        // Helper to OR two expressions
        private static System.Linq.Expressions.Expression<System.Func<ApplicationUser, bool>> Or(
            System.Linq.Expressions.Expression<System.Func<ApplicationUser, bool>> expr1,
            System.Linq.Expressions.Expression<System.Func<ApplicationUser, bool>> expr2)
        {
            var parameter = System.Linq.Expressions.Expression.Parameter(typeof(ApplicationUser));

            var leftVisitor = new ReplaceExpressionVisitor(expr1.Parameters[0], parameter);
            var left = leftVisitor.Visit(expr1.Body);

            var rightVisitor = new ReplaceExpressionVisitor(expr2.Parameters[0], parameter);
            var right = rightVisitor.Visit(expr2.Body);

            return System.Linq.Expressions.Expression.Lambda<System.Func<ApplicationUser, bool>>(
                System.Linq.Expressions.Expression.OrElse(left!, right!), parameter);
        }

        private sealed class ReplaceExpressionVisitor : System.Linq.Expressions.ExpressionVisitor
        {
            private readonly System.Linq.Expressions.Expression _oldValue;
            private readonly System.Linq.Expressions.Expression _newValue;

            public ReplaceExpressionVisitor(System.Linq.Expressions.Expression oldValue, System.Linq.Expressions.Expression newValue)
            {
                _oldValue = oldValue;
                _newValue = newValue;
            }

            public override System.Linq.Expressions.Expression? Visit(System.Linq.Expressions.Expression? node)
            {
                if (node == _oldValue)
                    return _newValue;
                return base.Visit(node);
            }
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