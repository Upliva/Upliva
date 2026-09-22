using Microsoft.EntityFrameworkCore;
using UplivaResortBooking.Data;
using UplivaResortBooking.Models;

namespace UplivaResortBooking.Services;

/// <summary>
/// Adds the platform tables without changing the existing resort migration.
/// This is intentionally idempotent so the committed resort/booking implementation remains intact.
/// </summary>
public static class PlatformSchemaInitializer
{
    public static async Task InitializeAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ResortDbContext>();

        const string sql = """
IF OBJECT_ID(N'dbo.Businesses', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Businesses
    (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Businesses PRIMARY KEY,
        Name NVARCHAR(180) NOT NULL,
        Slug NVARCHAR(80) NOT NULL,
        BusinessType NVARCHAR(80) NOT NULL,
        Status NVARCHAR(40) NOT NULL,
        IsPublished BIT NOT NULL CONSTRAINT DF_Businesses_IsPublished DEFAULT 0,
        OwnerName NVARCHAR(150) NOT NULL,
        Email NVARCHAR(200) NOT NULL,
        PhoneNumber NVARCHAR(30) NOT NULL,
        WhatsAppNumber NVARCHAR(30) NOT NULL,
        Address NVARCHAR(300) NOT NULL,
        City NVARCHAR(100) NOT NULL,
        Tagline NVARCHAR(250) NOT NULL,
        Description NVARCHAR(2000) NOT NULL,
        LogoUrl NVARCHAR(500) NOT NULL,
        HeroImageUrl NVARCHAR(500) NOT NULL,
        CreatedAtUtc DATETIME2 NOT NULL,
        ApprovedAtUtc DATETIME2 NULL,
        PublishedAtUtc DATETIME2 NULL
    );
    CREATE UNIQUE INDEX IX_Businesses_Slug ON dbo.Businesses(Slug);
END;

IF OBJECT_ID(N'dbo.PlatformUsers', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.PlatformUsers
    (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_PlatformUsers PRIMARY KEY,
        FullName NVARCHAR(150) NOT NULL,
        Email NVARCHAR(200) NOT NULL,
        PhoneNumber NVARCHAR(30) NOT NULL,
        PasswordHash NVARCHAR(MAX) NOT NULL,
        Role NVARCHAR(40) NOT NULL,
        BusinessId INT NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_PlatformUsers_IsActive DEFAULT 1,
        CreatedAtUtc DATETIME2 NOT NULL
    );
    CREATE UNIQUE INDEX IX_PlatformUsers_Email ON dbo.PlatformUsers(Email);
END;

IF OBJECT_ID(N'dbo.WebsiteConfigurations', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.WebsiteConfigurations
    (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_WebsiteConfigurations PRIMARY KEY,
        BusinessId INT NOT NULL,
        Theme NVARCHAR(40) NOT NULL,
        PrimaryColor NVARCHAR(20) NOT NULL,
        AccentColor NVARCHAR(20) NOT NULL,
        ShowOffers BIT NOT NULL,
        ShowTestimonials BIT NOT NULL,
        ShowCatalog BIT NOT NULL,
        ShowWhatsApp BIT NOT NULL
    );
    CREATE UNIQUE INDEX IX_WebsiteConfigurations_BusinessId ON dbo.WebsiteConfigurations(BusinessId);
END;

IF OBJECT_ID(N'dbo.BusinessCatalogItems', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.BusinessCatalogItems
    (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_BusinessCatalogItems PRIMARY KEY,
        BusinessId INT NOT NULL,
        Name NVARCHAR(150) NOT NULL,
        Description NVARCHAR(1000) NOT NULL,
        PriceText NVARCHAR(100) NOT NULL,
        ImageUrl NVARCHAR(500) NOT NULL,
        Category NVARCHAR(80) NOT NULL,
        SortOrder INT NOT NULL,
        IsActive BIT NOT NULL
    );
END;

IF OBJECT_ID(N'dbo.BusinessOffers', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.BusinessOffers
    (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_BusinessOffers PRIMARY KEY,
        BusinessId INT NOT NULL,
        Title NVARCHAR(180) NOT NULL,
        Description NVARCHAR(1000) NOT NULL,
        ImageUrl NVARCHAR(500) NOT NULL,
        DiscountText NVARCHAR(80) NOT NULL,
        StartsOn DATETIME2 NULL,
        EndsOn DATETIME2 NULL,
        IsPublished BIT NOT NULL
    );
END;

IF OBJECT_ID(N'dbo.BusinessTestimonials', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.BusinessTestimonials
    (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_BusinessTestimonials PRIMARY KEY,
        BusinessId INT NOT NULL,
        CustomerName NVARCHAR(120) NOT NULL,
        Feedback NVARCHAR(1000) NOT NULL,
        Location NVARCHAR(100) NOT NULL,
        IsDemo BIT NOT NULL,
        IsPublished BIT NOT NULL
    );
END;

IF OBJECT_ID(N'dbo.BusinessEnquiries', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.BusinessEnquiries
    (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_BusinessEnquiries PRIMARY KEY,
        BusinessId INT NOT NULL,
        Name NVARCHAR(150) NOT NULL,
        PhoneNumber NVARCHAR(30) NOT NULL,
        Email NVARCHAR(200) NOT NULL,
        Message NVARCHAR(2000) NOT NULL,
        Status NVARCHAR(40) NOT NULL,
        CreatedAtUtc DATETIME2 NOT NULL
    );
END;

IF OBJECT_ID(N'dbo.BusinessWhatsAppSettings', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.BusinessWhatsAppSettings
    (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_BusinessWhatsAppSettings PRIMARY KEY,
        BusinessId INT NOT NULL,
        WabaId NVARCHAR(100) NOT NULL,
        PhoneNumberId NVARCHAR(100) NOT NULL,
        AccessToken NVARCHAR(MAX) NOT NULL,
        WebhookVerifyToken NVARCHAR(100) NOT NULL,
        GraphApiVersion NVARCHAR(20) NOT NULL,
        IsEnabled BIT NOT NULL
    );
    CREATE UNIQUE INDEX IX_BusinessWhatsAppSettings_BusinessId ON dbo.BusinessWhatsAppSettings(BusinessId);
END;
""";

        await db.Database.ExecuteSqlRawAsync(sql);
        await SeedPlatformAsync(db);
    }

    private static async Task SeedPlatformAsync(ResortDbContext db)
    {
        // Do not create or publish sample businesses in the public platform.
        // Earlier development builds could have created Paradise Palm as a demo tenant.
        // Keep the existing resort implementation intact, but remove that sample from the
        // reusable public business directory by unpublishing it and its demo testimonials.
        var legacyDemo = await db.Businesses.FirstOrDefaultAsync(x =>
            x.Slug == "paradise-palm-resort" &&
            x.OwnerName == "Demo Business Owner");

        if (legacyDemo is null)
            return;

        legacyDemo.IsPublished = false;
        legacyDemo.PublishedAtUtc = null;

        var demoTestimonials = await db.BusinessTestimonials
            .Where(x => x.BusinessId == legacyDemo.Id && x.IsDemo)
            .ToListAsync();

        if (demoTestimonials.Count > 0)
            db.BusinessTestimonials.RemoveRange(demoTestimonials);

        await db.SaveChangesAsync();
    }

}
