using ServiceRequestApp.Data;
using ServiceRequestApp.Models;

namespace ServiceRequestApp.Services
{
    public class CategorySeedService
    {
        private readonly ApplicationDbContext _context;

        public CategorySeedService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task SeedCategoriesAsync()
        {
            if (_context.Categories.Any())
                return;

            var categories = new List<Category>
            {
                // Home & Garden
                new Category { Name = "Home Cleaning", Description = "Professional home cleaning services", Icon = "fas fa-broom", Color = "#28a745", IsActive = true },
                new Category { Name = "Plumbing", Description = "Plumbing repairs and installations", Icon = "fas fa-wrench", Color = "#007bff", IsActive = true },
                new Category { Name = "Electrical", Description = "Electrical repairs and installations", Icon = "fas fa-bolt", Color = "#ffc107", IsActive = true },
                new Category { Name = "Painting", Description = "Interior and exterior painting services", Icon = "fas fa-paint-brush", Color = "#6f42c1", IsActive = true },
                new Category { Name = "Carpentry", Description = "Woodwork and furniture repairs", Icon = "fas fa-hammer", Color = "#fd7e14", IsActive = true },
                new Category { Name = "Gardening", Description = "Landscaping and garden maintenance", Icon = "fas fa-seedling", Color = "#20c997", IsActive = true },
                new Category { Name = "AC Repair", Description = "Air conditioning repair and maintenance", Icon = "fas fa-snowflake", Color = "#17a2b8", IsActive = true },
                new Category { Name = "Pest Control", Description = "Pest control and extermination", Icon = "fas fa-bug", Color = "#dc3545", IsActive = true },

                // Technology & IT
                new Category { Name = "Computer Repair", Description = "Computer and laptop repair services", Icon = "fas fa-laptop", Color = "#6c757d", IsActive = true },
                new Category { Name = "Mobile Repair", Description = "Smartphone and tablet repair", Icon = "fas fa-mobile-alt", Color = "#343a40", IsActive = true },
                new Category { Name = "Software Development", Description = "Custom software and app development", Icon = "fas fa-code", Color = "#e83e8c", IsActive = true },
                new Category { Name = "Web Design", Description = "Website design and development", Icon = "fas fa-globe", Color = "#6f42c1", IsActive = true },
                new Category { Name = "IT Support", Description = "Technical support and troubleshooting", Icon = "fas fa-headset", Color = "#007bff", IsActive = true },
                new Category { Name = "Network Setup", Description = "Network installation and configuration", Icon = "fas fa-network-wired", Color = "#28a745", IsActive = true },

                // Transportation & Delivery
                new Category { Name = "Ride Sharing", Description = "Personal transportation services", Icon = "fas fa-car", Color = "#007bff", IsActive = true },
                new Category { Name = "Food Delivery", Description = "Food delivery and catering services", Icon = "fas fa-utensils", Color = "#fd7e14", IsActive = true },
                new Category { Name = "Package Delivery", Description = "Package and document delivery", Icon = "fas fa-box", Color = "#28a745", IsActive = true },
                new Category { Name = "Moving Services", Description = "Home and office moving assistance", Icon = "fas fa-truck", Color = "#6c757d", IsActive = true },

                // Beauty & Wellness
                new Category { Name = "Hair Styling", Description = "Hair cutting, styling, and coloring", Icon = "fas fa-cut", Color = "#e83e8c", IsActive = true },
                new Category { Name = "Makeup Artist", Description = "Professional makeup services", Icon = "fas fa-palette", Color = "#dc3545", IsActive = true },
                new Category { Name = "Massage Therapy", Description = "Therapeutic massage services", Icon = "fas fa-spa", Color = "#20c997", IsActive = true },
                new Category { Name = "Fitness Training", Description = "Personal fitness and training", Icon = "fas fa-dumbbell", Color = "#ffc107", IsActive = true },
                new Category { Name = "Yoga Instructor", Description = "Yoga and meditation classes", Icon = "fas fa-om", Color = "#6f42c1", IsActive = true },

                // Education & Tutoring
                new Category { Name = "Academic Tutoring", Description = "Subject-specific academic tutoring", Icon = "fas fa-graduation-cap", Color = "#007bff", IsActive = true },
                new Category { Name = "Language Learning", Description = "Language teaching and learning", Icon = "fas fa-language", Color = "#28a745", IsActive = true },
                new Category { Name = "Music Lessons", Description = "Music instrument and vocal training", Icon = "fas fa-music", Color = "#6f42c1", IsActive = true },
                new Category { Name = "Art Classes", Description = "Drawing, painting, and art instruction", Icon = "fas fa-paint-brush", Color = "#e83e8c", IsActive = true },
                new Category { Name = "Computer Training", Description = "Computer skills and software training", Icon = "fas fa-desktop", Color = "#6c757d", IsActive = true },

                // Business & Professional
                new Category { Name = "Accounting", Description = "Bookkeeping and accounting services", Icon = "fas fa-calculator", Color = "#28a745", IsActive = true },
                new Category { Name = "Legal Services", Description = "Legal consultation and document preparation", Icon = "fas fa-gavel", Color = "#dc3545", IsActive = true },
                new Category { Name = "Photography", Description = "Professional photography services", Icon = "fas fa-camera", Color = "#6c757d", IsActive = true },
                new Category { Name = "Video Production", Description = "Video recording and editing services", Icon = "fas fa-video", Color = "#007bff", IsActive = true },
                new Category { Name = "Marketing", Description = "Digital marketing and advertising", Icon = "fas fa-bullhorn", Color = "#fd7e14", IsActive = true },
                new Category { Name = "Translation", Description = "Document and text translation services", Icon = "fas fa-language", Color = "#6f42c1", IsActive = true },

                // Health & Medical
                new Category { Name = "Home Nursing", Description = "In-home nursing and care services", Icon = "fas fa-user-nurse", Color = "#dc3545", IsActive = true },
                new Category { Name = "Physiotherapy", Description = "Physical therapy and rehabilitation", Icon = "fas fa-heartbeat", Color = "#e83e8c", IsActive = true },
                new Category { Name = "Nutritionist", Description = "Nutritional counseling and meal planning", Icon = "fas fa-apple-alt", Color = "#28a745", IsActive = true },
                new Category { Name = "Mental Health", Description = "Counseling and mental health support", Icon = "fas fa-brain", Color = "#6f42c1", IsActive = true },

                // Automotive
                new Category { Name = "Car Repair", Description = "Automotive repair and maintenance", Icon = "fas fa-car-side", Color = "#6c757d", IsActive = true },
                new Category { Name = "Car Wash", Description = "Vehicle cleaning and detailing", Icon = "fas fa-car-wash", Color = "#17a2b8", IsActive = true },
                new Category { Name = "Driving Lessons", Description = "Driving instruction and training", Icon = "fas fa-steering-wheel", Color = "#007bff", IsActive = true },

                // Event Services
                new Category { Name = "Event Planning", Description = "Wedding and event coordination", Icon = "fas fa-calendar-alt", Color = "#e83e8c", IsActive = true },
                new Category { Name = "Catering", Description = "Food catering for events", Icon = "fas fa-utensils", Color = "#fd7e14", IsActive = true },
                new Category { Name = "DJ Services", Description = "Music and entertainment for events", Icon = "fas fa-music", Color = "#6f42c1", IsActive = true },
                new Category { Name = "Decoration", Description = "Event decoration and setup", Icon = "fas fa-gift", Color = "#28a745", IsActive = true }
            };

            _context.Categories.AddRange(categories);
            await _context.SaveChangesAsync();
        }
    }
}

