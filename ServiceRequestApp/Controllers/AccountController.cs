using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ServiceRequestApp.Models;
using ServiceRequestApp.ViewModels;
using System.Threading.Tasks;
using System.Linq;

namespace ServiceRequestApp.Controllers
{
    public partial class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        //Dependency Injection. These are services provided by ASP.NET core Identity.
        //for managing users and handling sign-ins.
        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }
        //Process the registration form. If the form is valid, create a new ApplicationUser object
        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = new ApplicationUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    Address = model.Address,
                    UserType = model.UserType,
                    PhoneNumber = model.PhoneNumber,
                    Zipcode = model.Zipcode,
                    NationalId = model.NationalId,
                    BusinessCredentials = model.UserType == "Provider" ? model.BusinessCredentials : null,
                    BusinessImagePath = model.UserType == "Provider" ? model.BusinessImagePath : null
                };

                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(user, model.UserType);
                    await _signInManager.SignInAsync(user, isPersistent: false);
                    return RedirectToAction("Index", "Home");
                }
                //If the registration fails, add the errors to the ModelState object.
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
            }
            return View(model);
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        //Process the login form. If the form is valid, attempt to sign in the user.
        [HttpPost]
        //async keyword allows the method to use the await keyword.
        //Task<IActionResult> is the return type of the method. It represents an asynchronous operation that returns an IActionResult object.
        //The await keyword is used to pause the execution of the method until the awaited task is complete.
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Fixed admin credentials
                if (model.Email == "admin@admin.com" && model.Password == "Admin@123")
                {
                    // Ensure admin user exists
                    var adminUser = await _userManager.FindByEmailAsync(model.Email);
                    if (adminUser == null)
                    {
                        adminUser = new ApplicationUser
                        {
                            UserName = model.Email,
                            Email = model.Email,
                            FirstName = "Admin",
                            LastName = "User",
                            UserType = "Admin"
                        };
                        await _userManager.CreateAsync(adminUser, model.Password);
                    }
                    // Always ensure admin role is assigned
                    if (!await _userManager.IsInRoleAsync(adminUser, "Admin"))
                    {
                        await _userManager.AddToRoleAsync(adminUser, "Admin");
                    }
                    await _signInManager.SignInAsync(adminUser, isPersistent: false);
                    return RedirectToAction("Dashboard", "Admin");
                }

                var result = await _signInManager.PasswordSignInAsync(
                    model.Email,
                    model.Password,
                    model.RememberMe,
                    lockoutOnFailure: false);

                if (result.Succeeded)
                {
                    return RedirectToAction("Index", "Home");
                }
                //If the login fails, add an error to the ModelState object.
                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            }
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}
