using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ServiceRequestApp.Models;
using ServiceRequestApp.ViewModels;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

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

        [HttpGet]
        public IActionResult RequesterRegister()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> RequesterRegister(RequesterRegisterViewModel model)
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
                    UserType = "Requester",
                    PhoneNumber = model.PhoneNumber,
                    Zipcode = model.Zipcode,
                    NationalId = model.NationalId
                };

                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(user, "Requester");
                    await _signInManager.SignInAsync(user, isPersistent: false);
                    return Json(new { success = true, message = "Registration successful! Welcome to our platform." });
                }
                
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
            }
            return Json(new { success = false, errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage) });
        }

        [HttpGet]
        public IActionResult ProviderRegister()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ProviderRegister(ProviderRegisterViewModel model)
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
                    UserType = "Provider",
                    PhoneNumber = model.PhoneNumber,
                    Zipcode = model.Zipcode,
                    NationalId = model.NationalId,
                    ShopName = model.ShopName,
                    ShopDescription = model.ShopDescription,
                    ShopAddress = model.ShopAddress,
                    ShopPhone = model.ShopPhone,
                    BusinessCredentials = model.BusinessCredentials,
                    BusinessImagePath = model.BusinessImagePath
                };

                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(user, "Provider");
                    await _signInManager.SignInAsync(user, isPersistent: false);
                    return Json(new { success = true, message = "Registration successful! Welcome to our business platform." });
                }
                
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
            }
            return Json(new { success = false, errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage) });
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

        [Authorize]
        public async Task<IActionResult> ProviderProfile(string id)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();
            var user = await _userManager.Users.FirstOrDefaultAsync(u => u.Id == id && u.UserType == "Provider");
            if (user == null) return NotFound();
            return View(user);
        }

        [Authorize]
        public async Task<IActionResult> RequesterProfile(string id)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();
            var user = await _userManager.Users.FirstOrDefaultAsync(u => u.Id == id && u.UserType == "Requester");
            if (user == null) return NotFound();
            return View(user);
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> AllProviders()
        {
            var providers = await _userManager.Users
                .Where(u => u.UserType == "Provider")
                .ToListAsync();
            return View(providers);
        }

        [HttpGet]
        public IActionResult TaskerRegister()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> TaskerRegister(TaskerRegisterViewModel model)
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
                    UserType = "Tasker",
                    PhoneNumber = model.PhoneNumber,
                    Zipcode = model.Zipcode,
                    NationalId = model.NationalId,
                    // Tasker-specific fields (store in custom fields or extend ApplicationUser if needed)
                    Skills = model.Skills,
                    PortfolioUrl = model.PortfolioUrl,
                    ProfileDescription = model.ProfileDescription
                };
                var result = await _userManager.CreateAsync(user, model.Password);
                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(user, "Tasker");
                    await _signInManager.SignInAsync(user, isPersistent: false);
                    return RedirectToAction("Index", "Home");
                }
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
            }
            return View(model);
        }

        [Authorize]
        public async Task<IActionResult> EditRequesterProfile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null || user.UserType != "Requester") return NotFound();
            var model = new EditRequesterProfileViewModel
            {
                FirstName = user.FirstName,
                LastName = user.LastName,
                Address = user.Address,
                PhoneNumber = user.PhoneNumber,
                Zipcode = user.Zipcode
            };
            return View(model);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> EditRequesterProfile(EditRequesterProfileViewModel model)
        {
            if (!ModelState.IsValid) return View(model);
            var user = await _userManager.GetUserAsync(User);
            if (user == null || user.UserType != "Requester") return NotFound();
            user.FirstName = model.FirstName;
            user.LastName = model.LastName;
            user.Address = model.Address;
            user.PhoneNumber = model.PhoneNumber;
            user.Zipcode = model.Zipcode;
            await _userManager.UpdateAsync(user);
            TempData["ProfileMessage"] = "Profile updated successfully.";
            return RedirectToAction("RequesterProfile", new { id = user.Id });
        }

        [Authorize]
        public async Task<IActionResult> EditProviderProfile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null || user.UserType != "Provider") return NotFound();
            var model = new EditProviderProfileViewModel
            {
                FirstName = user.FirstName,
                LastName = user.LastName,
                Address = user.Address,
                PhoneNumber = user.PhoneNumber,
                Zipcode = user.Zipcode,
                ShopName = user.ShopName,
                ShopDescription = user.ShopDescription,
                ShopAddress = user.ShopAddress,
                ShopPhone = user.ShopPhone,
                BusinessCredentials = user.BusinessCredentials,
                BusinessImagePath = user.BusinessImagePath
            };
            return View(model);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> EditProviderProfile(EditProviderProfileViewModel model)
        {
            if (!ModelState.IsValid) return View(model);
            var user = await _userManager.GetUserAsync(User);
            if (user == null || user.UserType != "Provider") return NotFound();
            user.FirstName = model.FirstName;
            user.LastName = model.LastName;
            user.Address = model.Address;
            user.PhoneNumber = model.PhoneNumber;
            user.Zipcode = model.Zipcode;
            user.ShopName = model.ShopName;
            user.ShopDescription = model.ShopDescription;
            user.ShopAddress = model.ShopAddress;
            user.ShopPhone = model.ShopPhone;
            user.BusinessCredentials = model.BusinessCredentials;
            user.BusinessImagePath = model.BusinessImagePath;
            await _userManager.UpdateAsync(user);
            TempData["ProfileMessage"] = "Profile updated successfully.";
            return RedirectToAction("ProviderProfile", new { id = user.Id });
        }

        [Authorize]
        public async Task<IActionResult> EditTaskerProfile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null || user.UserType != "Tasker") return NotFound();
            var model = new EditTaskerProfileViewModel
            {
                FirstName = user.FirstName,
                LastName = user.LastName,
                Address = user.Address,
                PhoneNumber = user.PhoneNumber,
                Zipcode = user.Zipcode,
                Skills = user.Skills,
                PortfolioUrl = user.PortfolioUrl,
                ProfileDescription = user.ProfileDescription
            };
            return View(model);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> EditTaskerProfile(EditTaskerProfileViewModel model)
        {
            if (!ModelState.IsValid) return View(model);
            var user = await _userManager.GetUserAsync(User);
            if (user == null || user.UserType != "Tasker") return NotFound();
            user.FirstName = model.FirstName;
            user.LastName = model.LastName;
            user.Address = model.Address;
            user.PhoneNumber = model.PhoneNumber;
            user.Zipcode = model.Zipcode;
            user.Skills = model.Skills;
            user.PortfolioUrl = model.PortfolioUrl;
            user.ProfileDescription = model.ProfileDescription;
            await _userManager.UpdateAsync(user);
            TempData["ProfileMessage"] = "Profile updated successfully.";
            return RedirectToAction("TaskerProfile", new { id = user.Id });
        }
    }
}
