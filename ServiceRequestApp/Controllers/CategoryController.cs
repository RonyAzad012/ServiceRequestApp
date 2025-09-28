using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ServiceRequestApp.Data;
using ServiceRequestApp.Models;
using ServiceRequestApp.Services;
using System.Linq;
using System.Threading.Tasks;

namespace ServiceRequestApp.Controllers
{
    [Authorize(Roles = "Admin")]
    public class CategoryController : Controller
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly CategorySeedService _categorySeedService;

        public CategoryController(ApplicationDbContext dbContext, CategorySeedService categorySeedService)
        {
            _dbContext = dbContext;
            _categorySeedService = categorySeedService;
        }

        public async Task<IActionResult> Index()
        {
            var categories = await _dbContext.Categories
                .OrderBy(c => c.Name)
                .ToListAsync();
            return View(categories);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View(new Category());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Category model)
        {
            if (string.IsNullOrWhiteSpace(model.Name))
            {
                ModelState.AddModelError("Name", "Category name is required.");
            }
            else if (await _dbContext.Categories.AnyAsync(c => c.Name.ToLower() == model.Name.ToLower()))
            {
                ModelState.AddModelError("Name", "A category with this name already exists.");
            }

            if (string.IsNullOrWhiteSpace(model.Icon))
            {
                model.Icon = "fas fa-tag"; // Default icon
            }

            if (string.IsNullOrWhiteSpace(model.Color))
            {
                model.Color = "#007bff"; // Default color
            }

            if (ModelState.IsValid)
            {
                model.CreatedAt = DateTime.UtcNow;
                model.IsActive = true;
                
                _dbContext.Categories.Add(model);
                await _dbContext.SaveChangesAsync();
                
                TempData["SuccessMessage"] = "Category added successfully.";
                return RedirectToAction("Index");
            }
            
            TempData["ErrorMessage"] = "Failed to add category. Please check for errors.";
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var category = await _dbContext.Categories.FindAsync(id);
            if (category == null) return NotFound();
            return View(category);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Category model)
        {
            if (string.IsNullOrWhiteSpace(model.Name))
            {
                ModelState.AddModelError("Name", "Category name is required.");
            }
            else if (await _dbContext.Categories.AnyAsync(c => c.Name.ToLower() == model.Name.ToLower() && c.Id != model.Id))
            {
                ModelState.AddModelError("Name", "A category with this name already exists.");
            }

            if (ModelState.IsValid)
            {
                var category = await _dbContext.Categories.FindAsync(model.Id);
                if (category == null) return NotFound();

                category.Name = model.Name;
                category.Description = model.Description;
                category.Icon = model.Icon;
                category.Color = model.Color;
                category.IsActive = model.IsActive;

                await _dbContext.SaveChangesAsync();
                TempData["SuccessMessage"] = "Category updated successfully.";
                return RedirectToAction("Index");
            }

            TempData["ErrorMessage"] = "Failed to update category. Please check for errors.";
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var category = await _dbContext.Categories.FindAsync(id);
            if (category == null)
                return Json(new { success = false, message = "Category not found" });

            category.IsActive = !category.IsActive;
            await _dbContext.SaveChangesAsync();

            return Json(new { 
                success = true, 
                message = $"Category {(category.IsActive ? "activated" : "deactivated")} successfully",
                isActive = category.IsActive
            });
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var category = await _dbContext.Categories.FindAsync(id);
            if (category == null)
                return Json(new { success = false, message = "Category not found" });

            // Check if category is being used
            var hasRequests = await _dbContext.ServiceRequests.AnyAsync(r => r.CategoryId == id);
            var hasProviders = await _dbContext.Users.AnyAsync(u => u.PrimaryCategoryId == id);

            if (hasRequests || hasProviders)
            {
                return Json(new { success = false, message = "Cannot delete category that is being used by service requests or providers" });
            }

            _dbContext.Categories.Remove(category);
            await _dbContext.SaveChangesAsync();
            
            return Json(new { success = true, message = "Category deleted successfully" });
        }

        [HttpPost]
        public async Task<IActionResult> SeedCategories()
        {
            try
            {
                await _categorySeedService.SeedCategoriesAsync();
                return Json(new { success = true, message = "Categories seeded successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Failed to seed categories: " + ex.Message });
            }
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

        [HttpGet]
        public async Task<IActionResult> GetPopularCategories()
        {
            var categories = await _dbContext.Categories
                .Where(c => c.IsActive)
                .Select(c => new { 
                    id = c.Id, 
                    name = c.Name, 
                    icon = c.Icon, 
                    color = c.Color,
                    providerCount = c.Providers.Count()
                })
                .OrderByDescending(c => c.providerCount)
                .ThenBy(c => c.name)
                .ToListAsync();
            
            return Json(new { success = true, categories });
        }
    }
}
