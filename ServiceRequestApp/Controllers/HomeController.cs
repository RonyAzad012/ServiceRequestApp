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
                .Where(u => u.UserType == "Provider")
                .AsQueryable();
            if (!string.IsNullOrEmpty(category))
            {
                providers = providers.Where(u => u.BusinessCredentials.Contains(category) || u.ShopName.Contains(category));
            }
            if (!string.IsNullOrEmpty(location))
            {
                providers = providers.Where(u => u.Address.Contains(location) || u.ShopAddress.Contains(location) || u.Zipcode.Contains(location));
            }
            var results = await providers.ToListAsync();
            ViewBag.Category = category;
            ViewBag.Location = location;
            return View("SearchResults", results);
        }
    }
}