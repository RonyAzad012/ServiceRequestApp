using ServiceRequestApp.Data;
using ServiceRequestApp.Models;
using Microsoft.EntityFrameworkCore;

namespace ServiceRequestApp.Services
{
    public interface ISearchService
    {
        Task<SearchResult<ApplicationUser>> SearchProvidersAsync(ProviderSearchCriteria criteria);
        Task<SearchResult<ServiceRequest>> SearchServiceRequestsAsync(ServiceRequestSearchCriteria criteria);
        Task<List<string>> GetSearchSuggestionsAsync(string query, string type);
        Task<List<ApplicationUser>> GetNearbyProvidersAsync(double latitude, double longitude, double radiusKm = 10);
    }

    public class SearchService : ISearchService
    {
        private readonly ApplicationDbContext _dbContext;

        public SearchService(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<SearchResult<ApplicationUser>> SearchProvidersAsync(ProviderSearchCriteria criteria)
        {
            var query = _dbContext.Users
                .Where(u => (u.UserType == "Provider" || u.UserType == "Business" || u.UserType == "Tasker") 
                           && u.IsApproved && u.IsAvailable)
                .Include(u => u.PrimaryCategory)
                .AsQueryable();

            // Category filter
            if (criteria.CategoryId.HasValue && criteria.CategoryId.Value > 0)
            {
                query = query.Where(u => u.PrimaryCategoryId == criteria.CategoryId.Value);
            }

            // Search term filter
            if (!string.IsNullOrEmpty(criteria.SearchTerm))
            {
                query = query.Where(u =>
                    u.ShopName.Contains(criteria.SearchTerm) ||
                    u.ProfileDescription.Contains(criteria.SearchTerm) ||
                    u.Skills.Contains(criteria.SearchTerm) ||
                    u.PrimaryCategory.Name.Contains(criteria.SearchTerm));
            }

            // Location filter
            if (!string.IsNullOrEmpty(criteria.Location))
            {
                query = query.Where(u =>
                    u.Address.Contains(criteria.Location) ||
                    u.ShopAddress.Contains(criteria.Location) ||
                    u.ServiceAreas.Contains(criteria.Location));
            }

            // Rating filter
            if (criteria.MinRating.HasValue)
            {
                query = query.Where(u => u.AverageRating >= (decimal)criteria.MinRating.Value);
            }

            // Price range filter
            if (criteria.MinPrice.HasValue || criteria.MaxPrice.HasValue)
            {
                // This would require a pricing table or service request analysis
                // For now, we'll skip this filter
            }

            // Distance filter (if coordinates provided)
            if (criteria.Latitude.HasValue && criteria.Longitude.HasValue && criteria.RadiusKm.HasValue)
            {
                // Calculate distance using Haversine formula
                query = query.Where(u => u.Latitude.HasValue && u.Longitude.HasValue &&
                    CalculateDistance(criteria.Latitude.Value, criteria.Longitude.Value, 
                                    u.Latitude.Value, u.Longitude.Value) <= criteria.RadiusKm.Value);
            }

            // Sorting
            query = criteria.SortBy?.ToLower() switch
            {
                "rating" => query.OrderByDescending(u => u.AverageRating),
                "reviews" => query.OrderByDescending(u => u.TotalReviews),
                "distance" when criteria.Latitude.HasValue && criteria.Longitude.HasValue =>
                    query.OrderBy(u => CalculateDistance(criteria.Latitude.Value, criteria.Longitude.Value, 
                                                       u.Latitude ?? 0, u.Longitude ?? 0)),
                "name" => query.OrderBy(u => u.ShopName),
                _ => query.OrderByDescending(u => u.AverageRating).ThenByDescending(u => u.TotalReviews)
            };

            var totalCount = await query.CountAsync();
            var items = await query
                .Skip((criteria.Page - 1) * criteria.PageSize)
                .Take(criteria.PageSize)
                .ToListAsync();

            return new SearchResult<ApplicationUser>
            {
                Items = items,
                TotalCount = totalCount,
                Page = criteria.Page,
                PageSize = criteria.PageSize,
                TotalPages = (int)Math.Ceiling((double)totalCount / criteria.PageSize)
            };
        }

        public async Task<SearchResult<ServiceRequest>> SearchServiceRequestsAsync(ServiceRequestSearchCriteria criteria)
        {
            var query = _dbContext.ServiceRequests
                .Include(sr => sr.Requester)
                .Include(sr => sr.Category)
                .AsQueryable();

            // Status filter
            if (!string.IsNullOrEmpty(criteria.Status))
            {
                query = query.Where(sr => sr.Status == criteria.Status);
            }

            // Category filter
            if (criteria.CategoryId.HasValue && criteria.CategoryId.Value > 0)
            {
                query = query.Where(sr => sr.CategoryId == criteria.CategoryId.Value);
            }

            // Search term filter
            if (!string.IsNullOrEmpty(criteria.SearchTerm))
            {
                query = query.Where(sr =>
                    sr.Title.Contains(criteria.SearchTerm) ||
                    sr.Description.Contains(criteria.SearchTerm) ||
                    sr.Category.Name.Contains(criteria.SearchTerm));
            }

            // Location filter
            if (!string.IsNullOrEmpty(criteria.Location))
            {
                query = query.Where(sr => sr.Address.Contains(criteria.Location));
            }

            // Price range filter
            if (criteria.MinPrice.HasValue)
            {
                query = query.Where(sr => sr.Budget >= criteria.MinPrice.Value);
            }
            if (criteria.MaxPrice.HasValue)
            {
                query = query.Where(sr => sr.Budget <= criteria.MaxPrice.Value);
            }

            // Date range filter
            if (criteria.StartDate.HasValue)
            {
                query = query.Where(sr => sr.CreatedAt >= criteria.StartDate.Value);
            }
            if (criteria.EndDate.HasValue)
            {
                query = query.Where(sr => sr.CreatedAt <= criteria.EndDate.Value);
            }

            // Sorting
            query = criteria.SortBy?.ToLower() switch
            {
                "price" => query.OrderBy(sr => sr.Budget),
                "price_desc" => query.OrderByDescending(sr => sr.Budget),
                "date" => query.OrderBy(sr => sr.CreatedAt),
                "date_desc" => query.OrderByDescending(sr => sr.CreatedAt),
                _ => query.OrderByDescending(sr => sr.CreatedAt)
            };

            var totalCount = await query.CountAsync();
            var items = await query
                .Skip((criteria.Page - 1) * criteria.PageSize)
                .Take(criteria.PageSize)
                .ToListAsync();

            return new SearchResult<ServiceRequest>
            {
                Items = items,
                TotalCount = totalCount,
                Page = criteria.Page,
                PageSize = criteria.PageSize,
                TotalPages = (int)Math.Ceiling((double)totalCount / criteria.PageSize)
            };
        }

        public async Task<List<string>> GetSearchSuggestionsAsync(string query, string type)
        {
            if (string.IsNullOrEmpty(query) || query.Length < 2)
                return new List<string>();

            var suggestions = new List<string>();

            if (type == "providers")
            {
                var providerSuggestions = await _dbContext.Users
                    .Where(u => (u.UserType == "Provider" || u.UserType == "Business" || u.UserType == "Tasker") 
                               && u.IsApproved)
                    .Where(u => u.ShopName.Contains(query) || u.Skills.Contains(query))
                    .Select(u => u.ShopName)
                    .Distinct()
                    .Take(5)
                    .ToListAsync();

                suggestions.AddRange(providerSuggestions);
            }
            else if (type == "categories")
            {
                var categorySuggestions = await _dbContext.Categories
                    .Where(c => c.IsActive && c.Name.Contains(query))
                    .Select(c => c.Name)
                    .Take(5)
                    .ToListAsync();

                suggestions.AddRange(categorySuggestions);
            }
            else if (type == "locations")
            {
                var locationSuggestions = await _dbContext.Users
                    .Where(u => u.Address.Contains(query) || u.ShopAddress.Contains(query))
                    .Select(u => u.Address)
                    .Distinct()
                    .Take(5)
                    .ToListAsync();

                suggestions.AddRange(locationSuggestions);
            }

            return suggestions;
        }

        public async Task<List<ApplicationUser>> GetNearbyProvidersAsync(double latitude, double longitude, double radiusKm = 10)
        {
            return await _dbContext.Users
                .Where(u => (u.UserType == "Provider" || u.UserType == "Business" || u.UserType == "Tasker") 
                           && u.IsApproved && u.IsAvailable
                           && u.Latitude.HasValue && u.Longitude.HasValue)
                .Where(u => CalculateDistance(latitude, longitude, u.Latitude.Value, u.Longitude.Value) <= radiusKm)
                .Include(u => u.PrimaryCategory)
                .OrderBy(u => CalculateDistance(latitude, longitude, u.Latitude.Value, u.Longitude.Value))
                .Take(20)
                .ToListAsync();
        }

        private double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
        {
            const double R = 6371; // Earth's radius in kilometers
            var dLat = ToRadians(lat2 - lat1);
            var dLon = ToRadians(lon2 - lon1);
            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return R * c;
        }

        private double ToRadians(double degrees)
        {
            return degrees * (Math.PI / 180);
        }
    }

    // Search Criteria Models
    public class ProviderSearchCriteria
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? SearchTerm { get; set; }
        public int? CategoryId { get; set; }
        public string? Location { get; set; }
        public double? MinRating { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public double? RadiusKm { get; set; }
        public string? SortBy { get; set; }
    }

    public class ServiceRequestSearchCriteria
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? SearchTerm { get; set; }
        public string? Status { get; set; }
        public int? CategoryId { get; set; }
        public string? Location { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? SortBy { get; set; }
    }

    public class SearchResult<T>
    {
        public List<T> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
    }
}

