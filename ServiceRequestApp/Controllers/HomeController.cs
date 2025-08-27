using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ServiceRequestApp.Models;
using System.Diagnostics;

namespace ServiceRequestApp.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
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
    }
}