using Microsoft.EntityFrameworkCore;
using UplivaAI.Models;

namespace UplivaAI.Data;

public class UplivaDbContext(DbContextOptions<UplivaDbContext> options) : DbContext(options)
{
    public DbSet<PlatformUser> PlatformUsers => Set<PlatformUser>();
    public DbSet<Business> Businesses => Set<Business>();
    public DbSet<BusinessCatalogItem> BusinessCatalogItems => Set<BusinessCatalogItem>();
    public DbSet<BusinessOffer> BusinessOffers => Set<BusinessOffer>();
    public DbSet<BusinessTestimonial> BusinessTestimonials => Set<BusinessTestimonial>();
    public DbSet<WebsiteConfiguration> WebsiteConfigurations => Set<WebsiteConfiguration>();
    public DbSet<BusinessEnquiry> BusinessEnquiries => Set<BusinessEnquiry>();
    public DbSet<BusinessWhatsAppSettings> BusinessWhatsAppSettings => Set<BusinessWhatsAppSettings>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<ErrorLog> ErrorLogs => Set<ErrorLog>();
    public DbSet<MarketingLead> ChatbotLeads => Set<MarketingLead>();
    public DbSet<PlatformVisit> PlatformVisits => Set<PlatformVisit>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<BusinessCatalogItem>()
            .Property(x => x.Rating)
            .HasPrecision(3, 2);

        modelBuilder.Entity<Business>()
            .HasIndex(x => x.Slug)
            .IsUnique();

        modelBuilder.Entity<PlatformUser>()
            .HasIndex(x => x.Email)
            .IsUnique();

        modelBuilder.Entity<WebsiteConfiguration>()
            .HasIndex(x => x.BusinessId)
            .IsUnique();

        modelBuilder.Entity<BusinessWhatsAppSettings>()
            .HasIndex(x => x.BusinessId)
            .IsUnique();

        modelBuilder.Entity<AuditLog>()
            .HasIndex(x => new { x.BusinessId, x.CreatedAtUtc });

        modelBuilder.Entity<AuditLog>()
            .HasIndex(x => x.CorrelationId);

        modelBuilder.Entity<Business>()
            .HasIndex(x => x.CustomDomain)
            .IsUnique()
            .HasFilter("[CustomDomain] IS NOT NULL");

        modelBuilder.Entity<Business>()
            .HasIndex(x => x.WhatsAppNumber)
            .IsUnique()
            .HasFilter("[WhatsAppNumber] IS NOT NULL AND [WhatsAppNumber] <> ''");

        modelBuilder.Entity<ErrorLog>()
            .HasIndex(x => new { x.BusinessId, x.CreatedAtUtc });

        modelBuilder.Entity<ErrorLog>()
            .HasIndex(x => x.CorrelationId);

        modelBuilder.Entity<BusinessCatalogItem>()
            .HasIndex(x => x.BusinessId);

        modelBuilder.Entity<BusinessCatalogItem>()
            .HasIndex(x => new { x.BusinessId, x.IsActive, x.ShowOnWebsite });

        modelBuilder.Entity<BusinessOffer>()
            .HasIndex(x => x.BusinessId);

        modelBuilder.Entity<BusinessEnquiry>()
            .HasIndex(x => x.BusinessId);

        modelBuilder.Entity<BusinessTestimonial>()
            .HasIndex(x => x.BusinessId);

        modelBuilder.Entity<MarketingLead>()
            .ToTable("ChatbotLeads");

        modelBuilder.Entity<MarketingLead>()
            .HasIndex(x => x.CreatedAtUtc);

        modelBuilder.Entity<MarketingLead>()
            .HasIndex(x => x.WhatsAppNumber);

        modelBuilder.Entity<MarketingLead>()
            .HasIndex(x => x.Status);

        modelBuilder.Entity<PlatformVisit>()
            .HasIndex(x => x.CreatedAtUtc);

        modelBuilder.Entity<PlatformVisit>()
            .HasIndex(x => x.VisitorId);
    }
}
