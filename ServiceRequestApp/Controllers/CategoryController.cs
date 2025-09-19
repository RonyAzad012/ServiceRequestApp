using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceRequestApp.Data;
using ServiceRequestApp.Models;
using System.Linq;
using System.Threading.Tasks;

namespace ServiceRequestApp.Controllers
{
    [Authorize(Roles = "Admin")]
    public class CategoryController : Controller
    {
        private readonly ApplicationDbContext _dbContext;
        public CategoryController(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public IActionResult Index()
        {
            var categories = _dbContext.Categories.ToList();
            return View(categories);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(Category model)
        {
            if (string.IsNullOrWhiteSpace(model.Name))
            {
                ModelState.AddModelError("Name", "Category name is required.");
            }
            else if (_dbContext.Categories.Any(c => c.Name == model.Name))
            {
                ModelState.AddModelError("Name", "A category with this name already exists.");
            }

            if (ModelState.IsValid)
            {
                _dbContext.Categories.Add(model);
                await _dbContext.SaveChangesAsync();
                TempData["CategoryMessage"] = "Category added successfully.";
                return RedirectToAction("Index");
            }
            TempData["CategoryMessage"] = "Failed to add category. Please check for errors.";
            return View(model);
        }

        public async Task<IActionResult> Delete(int id)
        {
            var category = await _dbContext.Categories.FindAsync(id);
            if (category == null) return NotFound();
            _dbContext.Categories.Remove(category);
            await _dbContext.SaveChangesAsync();
            return RedirectToAction("Index");
        }
    }
}
