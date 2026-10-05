using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.RateLimiting;
using Azure.Storage.Blobs;
using System.Threading.RateLimiting;
using UplivaAI.Data;
using UplivaAI.Models;
using UplivaAI.Services;
using UplivaAI.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Explicitly load the existing User Secrets store so the current flat keys
// such as "WhatsApp:PhoneNumberId" and "WhatsApp:AccessToken" are available
// even when the launch profile/environment is not Development.
builder.Configuration.AddUserSecrets<Program>(optional: true);
// Azure Storage credentials belong in User Secrets / environment configuration, never source control.

builder.Logging.ClearProviders();
builder.Logging.AddJsonConsole();
builder.Logging.AddDebug();

builder.Services.AddControllersWithViews();

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("public-enquiry", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(5),
                QueueLimit = 0,
                AutoReplenishment = true
            }));
    options.AddPolicy("login", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(5),
                QueueLimit = 0,
                AutoReplenishment = true
            }));
    options.AddPolicy("call-click", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 20,
                Window = TimeSpan.FromMinutes(5),
                QueueLimit = 0,
                AutoReplenishment = true
            }));
});
builder.Services.AddHttpContextAccessor();
builder.Services.AddMemoryCache(options => options.SizeLimit = 10_000);

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = ".UplivaAI.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.IsEssential = true;
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
builder.Services.Configure<AzureStorageOptions>(builder.Configuration.GetSection("AzureStorage"));
builder.Services.AddScoped<IBlobStorageService, AzureBlobStorageService>();
builder.Services.Configure<NotificationOptions>(builder.Configuration.GetSection("Notifications"));
builder.Services.AddScoped<IEmailService, SmtpEmailService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IIntegrationLogService, IntegrationLogService>();
builder.Services.AddHostedService<NotificationRetryWorker>();
builder.Services.AddHostedService<LogRetentionWorker>();

builder.Services.AddDbContext<UplivaDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sql => sql.EnableRetryOnFailure()));

builder.Services.Configure<WhatsAppSettings>(
    builder.Configuration.GetSection("WhatsApp"));
builder.Services.Configure<GupshupSettings>(
    builder.Configuration.GetSection("WhatsApp:Gupshup"));

builder.Services.AddHttpClient<IWhatsAppService, WhatsAppService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddHttpClient<IGupshupWhatsAppService, GupshupWhatsAppService>(client =>
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
app.UseRateLimiter();
app.UseAuthentication();
app.UseMiddleware<PasswordChangeRequiredMiddleware>();
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

app.MapGet("/health", async (UplivaDbContext db, IConfiguration configuration, CancellationToken cancellationToken) =>
{
    var sqlOk = false;
    try { sqlOk = await db.Database.CanConnectAsync(cancellationToken); } catch { }

    var azureConfigured = !string.IsNullOrWhiteSpace(configuration["AzureStorage:ConnectionString"]);
    var azureOk = false;
    if (azureConfigured)
    {
        try
        {
            var client = new BlobServiceClient(configuration["AzureStorage:ConnectionString"]);
            azureOk = await client.GetBlobContainerClient(configuration["AzureStorage:ContainerName"] ?? "upliva-media").ExistsAsync(cancellationToken);
        }
        catch { }
    }

    var gupshupConfigured = !string.IsNullOrWhiteSpace(configuration["WhatsApp:PhoneNumberId"]) &&
                            !string.IsNullOrWhiteSpace(configuration["WhatsApp:AccessToken"]);

    // Gupshup is optional in Phase 1, so it does not make the core platform unhealthy.
    var healthy = sqlOk && azureOk;
    return Results.Json(new
    {
        status = healthy ? "Healthy" : "Degraded",
        sql = sqlOk ? "Healthy" : "Unhealthy",
        azureBlob = azureOk ? "Healthy" : azureConfigured ? "Unhealthy" : "NotConfigured",
        gupshup = gupshupConfigured ? "Configured" : "NotConfigured",
        utc = DateTime.UtcNow
    }, statusCode: healthy ? StatusCodes.Status200OK : StatusCodes.Status503ServiceUnavailable);
});

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
