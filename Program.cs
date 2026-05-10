// ============================================================
// Program.cs — Application Entry Point
// ============================================================
// This is the root bootstrap file for the FitTrack ASP.NET Core
// MVC application. It configures all services (Dependency Injection),
// sets up the HTTP middleware pipeline, and starts the web server.
// ============================================================

using FitTrack.Data;
using FitTrack.Models;
using FitTrack.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

// Create the WebApplication builder — this sets up configuration sources
// (appsettings.json, environment variables, etc.) and prepares the DI container.
var builder = WebApplication.CreateBuilder(args);

// ── Database ──────────────────────────────────────────────────────────────────
// Register ApplicationDbContext (our EF Core database context) and point it
// at the PostgreSQL (Supabase) connection string defined in appsettings.json.
// All database queries in controllers and services go through this context.
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// ── ASP.NET Core Identity ─────────────────────────────────────────────────────
// Register Identity services for user authentication and role-based authorization.
// ApplicationUser is our custom user class; IdentityRole handles Admin/User roles.
// Password policy is relaxed here (no digit, uppercase, or symbol requirement)
// but a minimum of 8 characters is enforced.
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = false;
    options.Password.RequiredLength = 8;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.User.RequireUniqueEmail = true; // Prevent duplicate email registrations
})
.AddEntityFrameworkStores<ApplicationDbContext>() // Store identity data in our DB
.AddDefaultTokenProviders();                      // Enable password-reset tokens, etc.

// ── Cookie Authentication Settings ───────────────────────────────────────────
// Configure where Identity redirects unauthenticated or unauthorized users.
// LoginPath    → Redirect here when a [Authorize] route is accessed without login.
// AccessDeniedPath → Redirect here when a logged-in user lacks the required role.
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Home/AccessDenied";
});

// ── MVC & Application Services ────────────────────────────────────────────────
// Register the MVC framework (Controllers + Razor Views).
builder.Services.AddControllersWithViews();

// Register our custom QR Code service as Scoped (one instance per HTTP request).
// It is used to generate and verify tamper-proof QR codes for gym check-in.
builder.Services.AddScoped<MemberQrCodeService>();

// ── Build the Application ────────────────────────────────────────────────────
var app = builder.Build();

// ── Middleware Pipeline ───────────────────────────────────────────────────────
// Middleware runs in the order it is added below. Each piece of middleware
// either handles the request itself, or passes it to the next in line.

if (!app.Environment.IsDevelopment())
{
    // In production: use a custom error page instead of the detailed exception page.
    app.UseExceptionHandler("/Home/Error");
    // HSTS tells browsers to only use HTTPS for this domain for 30 days (default).
    app.UseHsts();
}

// Redirect all HTTP requests to HTTPS for secure communication.
app.UseHttpsRedirection();

// Serve static files (CSS, JS, images) from the wwwroot folder.
app.UseStaticFiles();

// Enable routing so the framework can match URLs to controller actions.
app.UseRouting();

// Identify the user from the authentication cookie (sets HttpContext.User).
app.UseAuthentication();

// Enforce [Authorize] attributes and role-based access checks.
app.UseAuthorization();

// Map static assets (Blazor/Razor Pages hybrid helper — harmless for pure MVC).
app.MapStaticAssets();

// ── Default Route ─────────────────────────────────────────────────────────────
// Map conventional MVC routes: {controller}/{action}/{id?}
// Default controller: Home, Default action: Index
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

// ── Database Seeding ──────────────────────────────────────────────────────────
// Run any pending EF Core migrations and create the default Admin account
// if it does not yet exist. This runs once at startup before handling requests.
await SeedData.InitializeAsync(app.Services);

// Start the web server and begin listening for HTTP requests.
app.Run();
