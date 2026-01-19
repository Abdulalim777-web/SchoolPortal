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

// ===================== MVC =====================
builder.Services.AddControllersWithViews();

// ===================== COOKIE =====================
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
});

var app = builder.Build();

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// Register navigation audit middleware to capture accessed/denied pages
app.UseMiddleware<SchoolPortal.Middleware.NavigationAuditMiddleware>();

// Ensure database is up-to-date, then seed roles/admin
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        // Apply any pending EF migrations
        var db = services.GetRequiredService<SchoolPortalDbContext>();
        await db.Database.MigrateAsync();

        // RoleSeeder is a static helper in Data/RoleSeeder.cs
        await RoleSeeder.SeedRolesAsync(services);
        await RoleSeeder.SeedAdminUserAsync(services);
    }
    catch (Exception ex)
    {
        // Log or ignore for now; seeding/migration failures should not stop the app from starting in dev
        Console.WriteLine($"Startup migration/seeding failed: {ex.Message}");
    }
}

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
