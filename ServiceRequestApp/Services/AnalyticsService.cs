using ServiceRequestApp.Data;
using ServiceRequestApp.Models;
using Microsoft.EntityFrameworkCore;

namespace ServiceRequestApp.Services
{
    public interface IAnalyticsService
    {
        Task<DashboardAnalytics> GetDashboardAnalyticsAsync();
        Task<RevenueAnalytics> GetRevenueAnalyticsAsync(DateTime startDate, DateTime endDate);
        Task<UserAnalytics> GetUserAnalyticsAsync();
        Task<ServiceAnalytics> GetServiceAnalyticsAsync();
        Task<List<PopularCategory>> GetPopularCategoriesAsync(int limit = 10);
        Task<List<TopProvider>> GetTopProvidersAsync(int limit = 10);
        Task<GeographicAnalytics> GetGeographicAnalyticsAsync();
    }

    public class AnalyticsService : IAnalyticsService
    {
        private readonly ApplicationDbContext _dbContext;

        public AnalyticsService(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<DashboardAnalytics> GetDashboardAnalyticsAsync()
        {
            var totalUsers = await _dbContext.Users.CountAsync();
            var totalProviders = await _dbContext.Users.CountAsync(u => u.UserType == "Provider" || u.UserType == "Business" || u.UserType == "Tasker");
            var totalRequests = await _dbContext.ServiceRequests.CountAsync();
            var completedRequests = await _dbContext.ServiceRequests.CountAsync(sr => sr.Status == "Completed");
            var totalRevenue = await _dbContext.PaymentTransactions
                .Where(pt => pt.Amount > 0 && pt.Status == "Completed")
                .SumAsync(pt => pt.Amount);
            var adminCommission = await _dbContext.PaymentTransactions
                .Where(pt => pt.Amount > 0 && pt.Status == "Completed")
                .SumAsync(pt => pt.AdminCommissionAmount ?? 0);

            // Monthly growth
            var currentMonth = DateTime.UtcNow.Month;
            var currentYear = DateTime.UtcNow.Year;
            var lastMonth = currentMonth == 1 ? 12 : currentMonth - 1;
            var lastMonthYear = currentMonth == 1 ? currentYear - 1 : currentYear;

            var currentMonthRequests = await _dbContext.ServiceRequests
                .CountAsync(sr => sr.CreatedAt.Month == currentMonth && sr.CreatedAt.Year == currentYear);
            var lastMonthRequests = await _dbContext.ServiceRequests
                .CountAsync(sr => sr.CreatedAt.Month == lastMonth && sr.CreatedAt.Year == lastMonthYear);

            var requestGrowth = lastMonthRequests > 0 
                ? ((double)(currentMonthRequests - lastMonthRequests) / lastMonthRequests) * 100 
                : 0;

            return new DashboardAnalytics
            {
                TotalUsers = totalUsers,
                TotalProviders = totalProviders,
                TotalRequests = totalRequests,
                CompletedRequests = completedRequests,
                TotalRevenue = totalRevenue,
                AdminCommission = adminCommission,
                RequestGrowthPercentage = Math.Round(requestGrowth, 1),
                CompletionRate = totalRequests > 0 ? Math.Round((double)completedRequests / totalRequests * 100, 1) : 0
            };
        }

        public async Task<RevenueAnalytics> GetRevenueAnalyticsAsync(DateTime startDate, DateTime endDate)
        {
            var transactions = await _dbContext.PaymentTransactions
                .Where(pt => pt.TransactionDate >= startDate && pt.TransactionDate <= endDate && pt.Amount > 0)
                .ToListAsync();

            var totalRevenue = transactions.Sum(t => t.Amount);
            var adminCommission = transactions.Sum(t => t.AdminCommissionAmount ?? 0);
            var providerEarnings = transactions.Sum(t => t.ProviderReceivedAmount ?? 0);

            // Daily revenue breakdown
            var dailyRevenue = transactions
                .GroupBy(t => t.TransactionDate.Date)
                .Select(g => new DailyRevenue
                {
                    Date = g.Key,
                    Revenue = g.Sum(t => t.Amount),
                    Commission = g.Sum(t => t.AdminCommissionAmount ?? 0)
                })
                .OrderBy(d => d.Date)
                .ToList();

            // Payment method breakdown
            var paymentMethodBreakdown = transactions
                .GroupBy(t => t.PaymentMethod)
                .Select(g => new PaymentMethodStats
                {
                    Method = g.Key,
                    Count = g.Count(),
                    Amount = g.Sum(t => t.Amount)
                })
                .ToList();

            return new RevenueAnalytics
            {
                TotalRevenue = totalRevenue,
                AdminCommission = adminCommission,
                ProviderEarnings = providerEarnings,
                DailyRevenue = dailyRevenue,
                PaymentMethodBreakdown = paymentMethodBreakdown,
                TransactionCount = transactions.Count
            };
        }

        public async Task<UserAnalytics> GetUserAnalyticsAsync()
        {
            var totalUsers = await _dbContext.Users.CountAsync();
            var activeUsers = await _dbContext.Users.CountAsync(u => u.IsApproved);
            var pendingApprovals = await _dbContext.Users.CountAsync(u => !u.IsApproved);

            var userTypeBreakdown = await _dbContext.Users
                .GroupBy(u => u.UserType)
                .Select(g => new UserTypeStats
                {
                    UserType = g.Key,
                    Count = g.Count()
                })
                .ToListAsync();

            // Monthly user registrations
            var monthlyRegistrations = await _dbContext.Users
                .Where(u => u.CreatedAt >= DateTime.UtcNow.AddMonths(-12))
                .GroupBy(u => new { u.CreatedAt.Year, u.CreatedAt.Month })
                .Select(g => new MonthlyStats
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    Count = g.Count()
                })
                .OrderBy(m => m.Year)
                .ThenBy(m => m.Month)
                .ToListAsync();

            return new UserAnalytics
            {
                TotalUsers = totalUsers,
                ActiveUsers = activeUsers,
                PendingApprovals = pendingApprovals,
                UserTypeBreakdown = userTypeBreakdown,
                MonthlyRegistrations = monthlyRegistrations
            };
        }

        public async Task<ServiceAnalytics> GetServiceAnalyticsAsync()
        {
            var totalRequests = await _dbContext.ServiceRequests.CountAsync();
            var completedRequests = await _dbContext.ServiceRequests.CountAsync(sr => sr.Status == "Completed");
            var pendingRequests = await _dbContext.ServiceRequests.CountAsync(sr => sr.Status == "Pending");
            var inProgressRequests = await _dbContext.ServiceRequests.CountAsync(sr => sr.Status == "In Progress");

            var averageCompletionTime = await _dbContext.ServiceRequests
                .Where(sr => sr.Status == "Completed" && sr.CompletedAt.HasValue)
                .Select(sr => EF.Functions.DateDiffDay(sr.CreatedAt, sr.CompletedAt.Value))
                .AverageAsync();

            var statusBreakdown = new List<StatusStats>
            {
                new StatusStats { Status = "Completed", Count = completedRequests },
                new StatusStats { Status = "Pending", Count = pendingRequests },
                new StatusStats { Status = "In Progress", Count = inProgressRequests }
            };

            return new ServiceAnalytics
            {
                TotalRequests = totalRequests,
                CompletedRequests = completedRequests,
                PendingRequests = pendingRequests,
                InProgressRequests = inProgressRequests,
                AverageCompletionTimeDays = Math.Round(averageCompletionTime, 1),
                StatusBreakdown = statusBreakdown
            };
        }

        public async Task<List<PopularCategory>> GetPopularCategoriesAsync(int limit = 10)
        {
            return await _dbContext.Categories
                .Where(c => c.IsActive)
                .Select(c => new PopularCategory
                {
                    CategoryId = c.Id,
                    CategoryName = c.Name,
                    RequestCount = c.ServiceRequests.Count(),
                    ProviderCount = c.Providers.Count(),
                    AverageRating = c.Providers.Where(p => p.AverageRating.HasValue).Average(p => p.AverageRating.Value)
                })
                .OrderByDescending(pc => pc.RequestCount)
                .Take(limit)
                .ToListAsync();
        }

        public async Task<List<TopProvider>> GetTopProvidersAsync(int limit = 10)
        {
            return await _dbContext.Users
                .Where(u => (u.UserType == "Provider" || u.UserType == "Business" || u.UserType == "Tasker") 
                           && u.IsApproved)
                .Select(u => new TopProvider
                {
                    ProviderId = u.Id,
                    ProviderName = u.ShopName ?? $"{u.FirstName} {u.LastName}",
                    CompletedRequests = u.ServiceRequests.Count(sr => sr.Status == "Completed"),
                    AverageRating = u.AverageRating ?? 0,
                    TotalReviews = u.TotalReviews ?? 0,
                    TotalEarnings = u.ServiceRequests
                        .Where(sr => sr.Status == "Completed" && sr.PaymentStatus == "Paid")
                        .Sum(sr => sr.ProviderAmount ?? 0)
                })
                .OrderByDescending(tp => tp.CompletedRequests)
                .ThenByDescending(tp => tp.AverageRating)
                .Take(limit)
                .ToListAsync();
        }

        public async Task<GeographicAnalytics> GetGeographicAnalyticsAsync()
        {
            var usersWithLocation = await _dbContext.Users
                .Where(u => u.Latitude.HasValue && u.Longitude.HasValue)
                .ToListAsync();

            var cityBreakdown = usersWithLocation
                .GroupBy(u => ExtractCityFromAddress(u.Address))
                .Select(g => new CityStats
                {
                    City = g.Key,
                    UserCount = g.Count(),
                    ProviderCount = g.Count(u => u.UserType == "Provider" || u.UserType == "Business" || u.UserType == "Tasker")
                })
                .OrderByDescending(c => c.UserCount)
                .Take(10)
                .ToList();

            return new GeographicAnalytics
            {
                TotalUsersWithLocation = usersWithLocation.Count,
                CityBreakdown = cityBreakdown
            };
        }

        private string ExtractCityFromAddress(string address)
        {
            if (string.IsNullOrEmpty(address))
                return "Unknown";

            // Simple city extraction - in a real app, you might use a more sophisticated approach
            var parts = address.Split(',');
            return parts.Length > 1 ? parts[parts.Length - 2].Trim() : "Unknown";
        }
    }

    // Analytics Models
    public class DashboardAnalytics
    {
        public int TotalUsers { get; set; }
        public int TotalProviders { get; set; }
        public int TotalRequests { get; set; }
        public int CompletedRequests { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal AdminCommission { get; set; }
        public double RequestGrowthPercentage { get; set; }
        public double CompletionRate { get; set; }
    }

    public class RevenueAnalytics
    {
        public decimal TotalRevenue { get; set; }
        public decimal AdminCommission { get; set; }
        public decimal ProviderEarnings { get; set; }
        public List<DailyRevenue> DailyRevenue { get; set; } = new();
        public List<PaymentMethodStats> PaymentMethodBreakdown { get; set; } = new();
        public int TransactionCount { get; set; }
    }

    public class UserAnalytics
    {
        public int TotalUsers { get; set; }
        public int ActiveUsers { get; set; }
        public int PendingApprovals { get; set; }
        public List<UserTypeStats> UserTypeBreakdown { get; set; } = new();
        public List<MonthlyStats> MonthlyRegistrations { get; set; } = new();
    }

    public class ServiceAnalytics
    {
        public int TotalRequests { get; set; }
        public int CompletedRequests { get; set; }
        public int PendingRequests { get; set; }
        public int InProgressRequests { get; set; }
        public double AverageCompletionTimeDays { get; set; }
        public List<StatusStats> StatusBreakdown { get; set; } = new();
    }

    public class PopularCategory
    {
        public int CategoryId { get; set; }
        public string CategoryName { get; set; }
        public int RequestCount { get; set; }
        public int ProviderCount { get; set; }
        public double AverageRating { get; set; }
    }

    public class TopProvider
    {
        public string ProviderId { get; set; }
        public string ProviderName { get; set; }
        public int CompletedRequests { get; set; }
        public double AverageRating { get; set; }
        public int TotalReviews { get; set; }
        public decimal TotalEarnings { get; set; }
    }

    public class GeographicAnalytics
    {
        public int TotalUsersWithLocation { get; set; }
        public List<CityStats> CityBreakdown { get; set; } = new();
    }

    // Supporting Models
    public class DailyRevenue
    {
        public DateTime Date { get; set; }
        public decimal Revenue { get; set; }
        public decimal Commission { get; set; }
    }

    public class PaymentMethodStats
    {
        public string Method { get; set; }
        public int Count { get; set; }
        public decimal Amount { get; set; }
    }

    public class UserTypeStats
    {
        public string UserType { get; set; }
        public int Count { get; set; }
    }

    public class MonthlyStats
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public int Count { get; set; }
    }

    public class StatusStats
    {
        public string Status { get; set; }
        public int Count { get; set; }
    }

    public class CityStats
    {
        public string City { get; set; }
        public int UserCount { get; set; }
        public int ProviderCount { get; set; }
    }
}

