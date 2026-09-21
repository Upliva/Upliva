using Microsoft.EntityFrameworkCore;
using UplivaResortBooking.Data;
using UplivaResortBooking.Models;
using UplivaResortBooking.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<ResortDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IBookingService, BookingService>();

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
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.MapControllers();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ResortDbContext>();
    await db.Database.MigrateAsync();
    await DbSeeder.SeedAsync(db);
}
app.MapGet("/privacy", () => Results.Content("""
<!DOCTYPE html>
<html>
<head>
    <title>Privacy Policy - Paradise Palm Resort</title>
    <meta charset="utf-8" />
</head>
<body>
    <h1>Privacy Policy</h1>

    <h2>Paradise Palm Resort</h2>

    <p>
        Paradise Palm Resort respects your privacy. This application is used
        to provide resort information, room availability, booking assistance,
        and customer support through WhatsApp.
    </p>

    <h2>Information We Collect</h2>
    <p>
        We may collect information such as your name, phone number, booking
        details, and messages when you contact us.
    </p>

    <h2>How We Use Information</h2>
    <p>
        Information is used to respond to customer enquiries, process
        bookings, provide customer support, and communicate booking-related
        information.
    </p>

    <h2>WhatsApp</h2>
    <p>
        Customer communications may be processed through WhatsApp and
        Meta's WhatsApp Business Platform.
    </p>

    <h2>Data Sharing</h2>
    <p>
        We do not sell customer personal information. Information may be
        processed by service providers required to operate our booking and
        messaging services.
    </p>

    <h2>Contact</h2>
    <p>
        For privacy questions, please contact Paradise Palm Resort.
    </p>
</body>
</html>
""", "text/html"));


app.Run();
