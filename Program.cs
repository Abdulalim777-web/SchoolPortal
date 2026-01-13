using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SchoolPortal.Data;
using SchoolPortal.Models;


var builder = WebApplication.CreateBuilder(args);

// ===================== DATABASE =====================
builder.Services.AddDbContext<SchoolPortalDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ===================== IDENTITY =====================
builder.Services.AddIdentity<User, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 6;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = false;
})
.AddEntityFrameworkStores<SchoolPortalDbContext>()
.AddDefaultTokenProviders();

// ===================== AUTHENTICATION (GOOGLE) =====================
// Only add Google authentication if configuration is present
var googleClientId = builder.Configuration["Authentication:Google:ClientId"];
var googleClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
if (!string.IsNullOrWhiteSpace(googleClientId) && !string.IsNullOrWhiteSpace(googleClientSecret))
{
    builder.Services.AddAuthentication()
        .AddGoogle(options =>
        {
            options.ClientId = googleClientId;
            options.ClientSecret = googleClientSecret;
            options.SaveTokens = true;
            options.Scope.Add("profile");
            options.Scope.Add("email");
        });
}

builder.Services.AddIdentity<User, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 6;
    options.Password.RequireUppercase = true;

    options.SignIn.RequireConfirmedEmail = true; // 🔴 IMPORTANT
})
.AddEntityFrameworkStores<SchoolPortalDbContext>()
.AddDefaultTokenProviders();

// ===================== MVC =====================
builder.Services.AddControllersWithViews();




// ===================== COOKIE =====================
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.SlidingExpiration = true;
});

var app = builder.Build();

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// Register navigation audit middleware to capture accessed/denied pages
app.UseMiddleware<SchoolPortal.Middleware.NavigationAuditMiddleware>();

// Seed roles and default admin user
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        // RoleSeeder is a static helper in Data/RoleSeeder.cs
        await RoleSeeder.SeedRolesAsync(services);
        await RoleSeeder.SeedAdminUserAsync(services);
    }
    catch (Exception ex)
    {
        // Log or ignore for now; seeding failures should not stop the app from starting in dev
        Console.WriteLine($"Role seeding failed: {ex.Message}");
    }
}

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
