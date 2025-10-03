using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ServiceRequestApp.Models;
using ServiceRequestApp.ViewModels;
using ServiceRequestApp.Data;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;

namespace ServiceRequestApp.Controllers
{
    public partial class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ApplicationDbContext _dbContext;

        //Dependency Injection. These are services provided by ASP.NET core Identity.
        //for managing users and handling sign-ins.
        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ApplicationDbContext dbContext)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _dbContext = dbContext;
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
                    Street = model.Street,
                    City = model.City,
                    UserType = "Requester",
                    PhoneNumber = model.PhoneNumber,
                    Zipcode = model.Zipcode,
                    NationalId = model.NationalId
                };
                user.Latitude = model.Latitude;
                user.Longitude = model.Longitude;

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
        public async Task<IActionResult> ProviderRegister()
        {
            var categories = await _dbContext.Categories.Where(c => c.IsActive).ToListAsync();
            ViewBag.Categories = categories;
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
                    Street = model.Street,
                    City = model.City,
                    UserType = "Provider",
                    PhoneNumber = model.PhoneNumber,
                    Zipcode = model.Zipcode,
                    NationalId = model.NationalId,
                    ShopName = model.ShopName,
                    ShopDescription = model.ShopDescription,
                    ShopAddress = model.ShopAddress,
                    ShopPhone = model.ShopPhone,
                    BusinessCredentials = model.BusinessCredentials,
                    BusinessImagePath = model.BusinessImagePath,
                    BusinessWebsite = model.BusinessWebsite,
                    PrimaryCategoryId = model.PrimaryCategoryId,
                    ServiceAreas = model.ServiceAreas,
                    IsApproved = false // Require admin approval
                };
                user.Latitude = model.Latitude;
                user.Longitude = model.Longitude;

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

        [Authorize]
        public async Task<IActionResult> TaskerProfile(string id)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();
            var user = await _userManager.Users.FirstOrDefaultAsync(u => u.Id == id && u.UserType == "Tasker");
            if (user == null) return NotFound();
            return View(user);
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> AllProviders(string category, string city, string rating, string search)
        {
            var query = _userManager.Users
                .Where(u => u.UserType == "Provider")
                .Include(u => u.Reviews)
                .Include(u => u.PrimaryCategory)
                .AsQueryable();

            // Apply filters
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(u => 
                    u.FirstName.Contains(search) || 
                    u.LastName.Contains(search) ||
                    u.ShopName.Contains(search) ||
                    u.ShopDescription.Contains(search));
            }

            if (!string.IsNullOrEmpty(city))
            {
                query = query.Where(u => u.City == city);
            }

            if (!string.IsNullOrEmpty(rating) && int.TryParse(rating, out int minRating))
            {
                query = query.Where(u => u.Reviews.Any() && 
                    u.Reviews.Average(r => r.Rating) >= minRating);
            }

            if (!string.IsNullOrEmpty(category))
            {
                // Filter providers by their primary category
                query = query.Where(u => u.PrimaryCategory != null && u.PrimaryCategory.Name == category);
            }

            var providers = await query.ToListAsync();

            // Get filter options
            var cities = await _userManager.Users
                .Where(u => u.UserType == "Provider" && !string.IsNullOrEmpty(u.City))
                .Select(u => u.City)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();

            var categories = await _dbContext.Categories
                .Where(c => c.IsActive && _userManager.Users.Any(u => u.UserType == "Provider" && u.PrimaryCategoryId == c.Id))
                .OrderBy(c => c.Name)
                .ToListAsync();

            ViewBag.Cities = cities;
            ViewBag.Categories = categories;
            ViewBag.SelectedCategory = category;
            ViewBag.SelectedCity = city;
            ViewBag.SelectedRating = rating;
            ViewBag.SearchTerm = search;

            return View(providers);
        }

        [HttpGet]
        public async Task<IActionResult> TaskerRegister()
        {
            var categories = await _dbContext.Categories.Where(c => c.IsActive).ToListAsync();
            ViewBag.Categories = categories;
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
                    Street = model.Street,
                    City = model.City,
                    UserType = "Tasker",
                    PhoneNumber = model.PhoneNumber,
                    Zipcode = model.Zipcode,
                    NationalId = model.NationalId,
                    // Tasker-specific fields
                    Skills = model.Skills,
                    PortfolioUrl = model.PortfolioUrl,
                    ProfileDescription = model.ProfileDescription,
                    PrimaryCategoryId = model.PrimaryCategoryId,
                    ServiceAreas = model.ServiceAreas,
                    IsApproved = false // Require admin approval
                };
                var result = await _userManager.CreateAsync(user, model.Password);
                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(user, "Tasker");
                    await _signInManager.SignInAsync(user, isPersistent: false);
                    return Json(new { success = true, message = "Registration successful! Welcome to our tasker platform." });
                }
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
            }
            return Json(new { success = false, errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage) });
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
                Street = user.Street,
                City = user.City,
                PhoneNumber = user.PhoneNumber,
                Zipcode = user.Zipcode,
                Latitude = user.Latitude,
                Longitude = user.Longitude
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
            user.Street = model.Street;
            user.City = model.City;
            user.PhoneNumber = model.PhoneNumber;
            user.Zipcode = model.Zipcode;
            user.Latitude = model.Latitude;
            user.Longitude = model.Longitude;
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
                Street = user.Street,
                City = user.City,
                PhoneNumber = user.PhoneNumber,
                Zipcode = user.Zipcode,
                Latitude = user.Latitude,
                Longitude = user.Longitude,
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
            user.Street = model.Street;
            user.City = model.City;
            user.PhoneNumber = model.PhoneNumber;
            user.Zipcode = model.Zipcode;
            user.Latitude = model.Latitude;
            user.Longitude = model.Longitude;
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
                Street = user.Street,
                City = user.City,
                PhoneNumber = user.PhoneNumber,
                Zipcode = user.Zipcode,
                Latitude = user.Latitude,
                Longitude = user.Longitude,
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
            user.Street = model.Street;
            user.City = model.City;
            user.PhoneNumber = model.PhoneNumber;
            user.Zipcode = model.Zipcode;
            user.Latitude = model.Latitude;
            user.Longitude = model.Longitude;
            user.Skills = model.Skills;
            user.PortfolioUrl = model.PortfolioUrl;
            user.ProfileDescription = model.ProfileDescription;
            await _userManager.UpdateAsync(user);
            TempData["ProfileMessage"] = "Profile updated successfully.";
            return RedirectToAction("TaskerProfile", new { id = user.Id });
        }

        // Dashboard API endpoints
        [HttpGet]
        public async Task<IActionResult> GetProviderStats(string providerId)
        {
            try
            {
                var totalRequests = await _dbContext.ServiceRequests
                    .Include(sr => sr.AcceptedRequest)
                    .CountAsync(sr => sr.AcceptedRequest.ProviderId == providerId);
                
                var completedRequests = await _dbContext.ServiceRequests
                    .Include(sr => sr.AcceptedRequest)
                    .CountAsync(sr => sr.AcceptedRequest.ProviderId == providerId && sr.Status == "Completed");
                
                var pendingRequests = await _dbContext.ServiceRequests
                    .Include(sr => sr.AcceptedRequest)
                    .CountAsync(sr => sr.AcceptedRequest.ProviderId == providerId && sr.Status == "In Progress");

                // Calculate total earnings for provider
                var totalEarnings = await _dbContext.ServiceRequests
                    .Include(sr => sr.AcceptedRequest)
                    .Where(sr => sr.AcceptedRequest.ProviderId == providerId && sr.PaymentStatus == "Paid")
                    .SumAsync(sr => sr.ProviderAmount ?? 0);

                // Calculate average rating received from requesters
                var averageRating = await _dbContext.Reviews
                    .Where(r => r.RevieweeId == providerId)
                    .AverageAsync(r => (double?)r.Rating) ?? 0;

                return Json(new { 
                    success = true, 
                    totalRequests, 
                    completedRequests, 
                    pendingRequests, 
                    averageRating,
                    totalEarnings 
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetRequesterStats(string requesterId)
        {
            try
            {
                var totalRequests = await _dbContext.ServiceRequests
                    .CountAsync(sr => sr.RequesterId == requesterId);
                
                var completedRequests = await _dbContext.ServiceRequests
                    .CountAsync(sr => sr.RequesterId == requesterId && sr.Status == "Completed");
                
                var pendingRequests = await _dbContext.ServiceRequests
                    .CountAsync(sr => sr.RequesterId == requesterId && sr.Status == "In Progress");

                // Calculate total amount spent by requester
                var totalSpent = await _dbContext.PaymentTransactions
                    .Where(pt => pt.UserId == requesterId && pt.Status == "Completed" && pt.Amount > 0)
                    .SumAsync(pt => pt.Amount);

                // Calculate average rating given to providers (not received from providers)
                var averageRating = await _dbContext.Reviews
                    .Where(r => r.ReviewerId == requesterId)
                    .AverageAsync(r => (double?)r.Rating) ?? 0;

                return Json(new { 
                    success = true, 
                    totalRequests, 
                    completedRequests, 
                    pendingRequests, 
                    averageRating,
                    totalSpent
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }


        [HttpGet]
        public async Task<IActionResult> GetTaskerStats(string taskerId)
        {
            try
            {
                var completedTasks = await _dbContext.ServiceRequests
                    .CountAsync(sr => sr.ProviderId == taskerId && sr.Status == "Completed");
                
                var activeTasks = await _dbContext.ServiceRequests
                    .CountAsync(sr => sr.ProviderId == taskerId && sr.Status == "In Progress");
                
                var totalEarnings = await _dbContext.ServiceRequests
                    .Where(sr => sr.ProviderId == taskerId && sr.Status == "Completed" && sr.PaymentAmount.HasValue)
                    .SumAsync(sr => sr.ProviderAmount ?? 0);

                return Json(new { 
                    success = true, 
                    completedTasks, 
                    activeTasks, 
                    totalEarnings 
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetRecentRequests(string providerId = null, string requesterId = null)
        {
            try
            {
                var query = _dbContext.ServiceRequests
                    .Include(sr => sr.AcceptedRequest)
                    .ThenInclude(ar => ar.Provider)
                    .AsQueryable();
                
                if (!string.IsNullOrEmpty(providerId))
                {
                    query = query.Where(sr => sr.ProviderId == providerId);
                }
                else if (!string.IsNullOrEmpty(requesterId))
                {
                    query = query.Where(sr => sr.RequesterId == requesterId);
                }

                var requests = await query
                    .OrderByDescending(sr => sr.CreatedAt)
                    .Take(5)
                    .ToListAsync();

                var currentUser = await _userManager.GetUserAsync(User);
                var requestData = requests.Select(sr => new {
                    sr.Id,
                    sr.Title,
                    sr.Description,
                    sr.Status,
                    sr.CreatedAt,
                    providerId = sr.AcceptedRequest?.ProviderId,
                    providerName = sr.AcceptedRequest?.Provider != null ? 
                        $"{sr.AcceptedRequest.Provider.FirstName} {sr.AcceptedRequest.Provider.LastName}" : null,
                    hasReview = currentUser != null && _dbContext.Reviews
                        .Any(r => r.ServiceRequestId == sr.Id && r.ReviewerId == currentUser.Id)
                }).ToList();

                return Json(new { success = true, requests = requestData });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetRecentTasks(string taskerId)
        {
            try
            {
                var tasks = await _dbContext.ServiceRequests
                    .Where(sr => sr.ProviderId == taskerId)
                    .OrderByDescending(sr => sr.CreatedAt)
                    .Take(5)
                    .Select(sr => new {
                        sr.Id,
                        sr.Title,
                        sr.Description,
                        sr.Status,
                        sr.CreatedAt
                    })
                    .ToListAsync();

                return Json(new { success = true, tasks });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetTaskerPerformance(string taskerId)
        {
            try
            {
                var totalRequests = await _dbContext.ServiceRequests
                    .CountAsync(sr => sr.ProviderId == taskerId);
                
                var respondedRequests = await _dbContext.ServiceRequests
                    .CountAsync(sr => sr.ProviderId == taskerId && sr.Status != "Pending");
                
                var completedRequests = await _dbContext.ServiceRequests
                    .CountAsync(sr => sr.ProviderId == taskerId && sr.Status == "Completed");

                var responseRate = totalRequests > 0 ? (respondedRequests * 100.0 / totalRequests) : 0;
                var completionRate = totalRequests > 0 ? (completedRequests * 100.0 / totalRequests) : 0;

                return Json(new { 
                    success = true, 
                    responseRate = Math.Round(responseRate, 1), 
                    completionRate = Math.Round(completionRate, 1) 
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> UploadProfileImage(IFormFile profileImage, string userId)
        {
            try
            {
                if (profileImage == null || profileImage.Length == 0)
                {
                    return Json(new { success = false, message = "No image file provided" });
                }

                // Validate file type
                var allowedTypes = new[] { "image/jpeg", "image/jpg", "image/png", "image/gif" };
                if (!allowedTypes.Contains(profileImage.ContentType.ToLower()))
                {
                    return Json(new { success = false, message = "Invalid file type. Only JPEG, PNG, and GIF images are allowed." });
                }

                // Validate file size (5MB max)
                if (profileImage.Length > 5 * 1024 * 1024)
                {
                    return Json(new { success = false, message = "File size too large. Maximum size is 5MB." });
                }

                // Get the current user
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null || currentUser.Id != userId)
                {
                    return Json(new { success = false, message = "Unauthorized" });
                }

                // Create uploads directory if it doesn't exist
                var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "profiles");
                if (!Directory.Exists(uploadsPath))
                {
                    Directory.CreateDirectory(uploadsPath);
                }

                // Generate unique filename
                var fileExtension = Path.GetExtension(profileImage.FileName);
                var fileName = $"{userId}_{DateTime.UtcNow.Ticks}{fileExtension}";
                var filePath = Path.Combine(uploadsPath, fileName);

                // Save the file
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await profileImage.CopyToAsync(stream);
                }

                // Update user's profile image path
                var relativePath = $"/uploads/profiles/{fileName}";
                currentUser.ProfileImagePath = relativePath;
                await _userManager.UpdateAsync(currentUser);

                return Json(new { success = true, imagePath = relativePath });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "An error occurred while uploading the image" });
            }
        }
    }
}
