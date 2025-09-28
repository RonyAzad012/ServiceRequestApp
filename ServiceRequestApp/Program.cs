using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ServiceRequestApp.Data;
using ServiceRequestApp.Models;
using ServiceRequestApp.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Register custom services
builder.Services.AddScoped<CategorySeedService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<ISSLCommerzPaymentService, SSLCommerzPaymentService>();
builder.Services.AddScoped<IServiceRequestCompletionService, ServiceRequestCompletionService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<ISearchService, SearchService>();
builder.Services.AddScoped<IReviewService, ReviewService>();
builder.Services.AddScoped<IAnalyticsService, AnalyticsService>();
builder.Services.AddHttpClient<PaymentService>();
builder.Services.AddHttpClient<SSLCommerzPaymentService>();

// Add DbContext with explicit options
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlServerOptionsAction: sqlOptions =>
        {
            sqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(30),
                errorNumbersToAdd: null);
        });
});

// Add Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 8;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Seed Roles and Categories
using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var roles = new[] { "Provider", "Requester", "Admin", "Tasker" };

    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole(role));
        }
    }

    // Seed categories using the service
    var categorySeedService = scope.ServiceProvider.GetRequiredService<CategorySeedService>();
    await categorySeedService.SeedCategoriesAsync();
}

app.Run();