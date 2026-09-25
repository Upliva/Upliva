using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using UplivaAI.Data;
using UplivaAI.Models;
using UplivaAI.Services;
using UplivaAI.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Explicitly load the existing User Secrets store so the current flat keys
// such as "WhatsApp:PhoneNumberId" and "WhatsApp:AccessToken" are available
// even when the launch profile/environment is not Development.
builder.Configuration.AddUserSecrets<Program>(optional: true);

builder.Logging.ClearProviders();
builder.Logging.AddJsonConsole();
builder.Logging.AddDebug();

builder.Services.AddControllersWithViews();
builder.Services.AddHttpContextAccessor();
builder.Services.AddMemoryCache(options => options.SizeLimit = 10_000);

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = ".UplivaAI.Auth";
        options.LoginPath = "/login";
        options.AccessDeniedPath = "/login";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    });

builder.Services.AddAuthorization();
builder.Services.AddScoped<IPasswordHasher<PlatformUser>, PasswordHasher<PlatformUser>>();
builder.Services.AddScoped<IPlatformAuthService, PlatformAuthService>();
builder.Services.AddScoped<IBusinessService, BusinessService>();
builder.Services.AddSingleton<ICatalogTemplateService, CatalogTemplateService>();
builder.Services.AddScoped<ICatalogImportService, CatalogImportService>();
builder.Services.AddScoped<IAuditLogService, AuditLogService>();
builder.Services.AddScoped<IErrorLogService, ErrorLogService>();
builder.Services.AddScoped<IMarketingEngagementService, MarketingEngagementService>();
builder.Services.AddSingleton<IBusinessCacheService, BusinessCacheService>();

builder.Services.AddDbContext<UplivaDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sql => sql.EnableRetryOnFailure()));

builder.Services.Configure<WhatsAppSettings>(
    builder.Configuration.GetSection("WhatsApp"));

builder.Services.AddHttpClient<IWhatsAppService, WhatsAppService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddScoped<IWhatsAppFlowService, WhatsAppFlowService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapControllerRoute(
    name: "register",
    pattern: "register",
    defaults: new { controller = "BusinessRegistration", action = "Register" });

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Database schema is managed through Entity Framework Core migrations.
// This WhatsApp-first refactor intentionally starts a new schema contract.
// Run in Visual Studio Package Manager Console:
//   Add-Migration WhatsAppFirstMvp -OutputDir Migrations
//   Update-Database
// The admin account is seeded from Admin:* User Secrets after the database exists.
await PlatformAdminSeeder.SeedAsync(app.Services, builder.Configuration);

if (app.Environment.IsDevelopment() &&
    (string.IsNullOrWhiteSpace(builder.Configuration["Admin:Email"]) ||
     string.IsNullOrWhiteSpace(builder.Configuration["Admin:Password"])))
{
    app.Logger.LogWarning("UplivaAI admin account is not configured. Set Admin:Email and Admin:Password with dotnet user-secrets, then restart the application.");
}

app.MapGet("/privacy", () => Results.Content("""
<!DOCTYPE html>
<html>
<head>
    <title>Privacy Policy - UplivaAI</title>
    <meta charset="utf-8" />
</head>
<body>
    <h1>Privacy Policy</h1>
    <h2>UplivaAI</h2>
    <p>UplivaAI provides business websites, customer enquiry tools and WhatsApp-based communication workflows.</p>
    <h2>Information We Collect</h2>
    <p>Business registration may collect business name, owner name, email, phone number, WhatsApp number, address and business information. Customer enquiries may also be collected by a business using the platform.</p>
    <h2>How We Use Information</h2>
    <p>Information is used to review business registrations, configure business websites, respond to enquiries and provide requested customer communications.</p>
    <h2>WhatsApp</h2>
    <p>Where enabled, communications may be processed through Meta's WhatsApp Business Platform.</p>
    <h2>Data Sharing</h2>
    <p>UplivaAI does not sell customer personal information. Service providers required to operate hosting, databases and messaging may process information on behalf of the platform or participating businesses.</p>
    <h2>Contact</h2>
    <p>For privacy questions, contact the UplivaAI platform administrator.</p>
</body>
</html>
""", "text/html"));

app.Run();
