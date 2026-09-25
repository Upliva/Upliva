using Microsoft.EntityFrameworkCore;
using UplivaAI.Models;

namespace UplivaAI.Data;

public class UplivaDbContext(DbContextOptions<UplivaDbContext> options) : DbContext(options)
{
    public DbSet<PlatformUser> PlatformUsers => Set<PlatformUser>();
    public DbSet<Business> Businesses => Set<Business>();
    public DbSet<BusinessCatalogItem> BusinessCatalogItems => Set<BusinessCatalogItem>();
    public DbSet<BusinessOffer> BusinessOffers => Set<BusinessOffer>();
    public DbSet<BusinessEnquiry> BusinessEnquiries => Set<BusinessEnquiry>();
    public DbSet<BusinessFollowUp> BusinessFollowUps => Set<BusinessFollowUp>();
    public DbSet<BusinessWhatsAppSettings> BusinessWhatsAppSettings => Set<BusinessWhatsAppSettings>();
    public DbSet<WhatsAppMessageLog> WhatsAppMessageLogs => Set<WhatsAppMessageLog>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<ErrorLog> ErrorLogs => Set<ErrorLog>();
    public DbSet<MarketingLead> ChatbotLeads => Set<MarketingLead>();
    public DbSet<PlatformVisit> PlatformVisits => Set<PlatformVisit>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<BusinessCatalogItem>().Property(x => x.Rating).HasPrecision(3, 2);
        modelBuilder.Entity<Business>().HasIndex(x => x.Slug).IsUnique();
        modelBuilder.Entity<PlatformUser>().HasIndex(x => x.Email).IsUnique();
        modelBuilder.Entity<BusinessWhatsAppSettings>().HasIndex(x => x.BusinessId).IsUnique();
        modelBuilder.Entity<BusinessWhatsAppSettings>().HasIndex(x => x.PhoneNumberId).IsUnique();
        modelBuilder.Entity<AuditLog>().HasIndex(x => new { x.BusinessId, x.CreatedAtUtc });
        modelBuilder.Entity<AuditLog>().HasIndex(x => x.CorrelationId);
        modelBuilder.Entity<Business>().HasIndex(x => x.WhatsAppNumber).IsUnique().HasFilter("[WhatsAppNumber] IS NOT NULL AND [WhatsAppNumber] <> ''");
        modelBuilder.Entity<BusinessCatalogItem>().Property(x => x.CustomAttributesJson).HasColumnType("nvarchar(max)");
        modelBuilder.Entity<BusinessCatalogItem>().HasIndex(x => x.BusinessId);
        modelBuilder.Entity<BusinessCatalogItem>().HasIndex(x => new { x.BusinessId, x.IsActive, x.IsWhatsAppTopPick, x.SortOrder });
        modelBuilder.Entity<BusinessOffer>().HasIndex(x => new { x.BusinessId, x.IsActive });
        modelBuilder.Entity<BusinessEnquiry>().HasIndex(x => new { x.BusinessId, x.CreatedAtUtc });
        modelBuilder.Entity<BusinessEnquiry>().HasIndex(x => x.WhatsAppMessageId);
        modelBuilder.Entity<WhatsAppMessageLog>().HasIndex(x => new { x.BusinessId, x.CreatedAtUtc });
        modelBuilder.Entity<WhatsAppMessageLog>().HasIndex(x => new { x.BusinessId, x.ExternalMessageId }).IsUnique().HasFilter("[ExternalMessageId] IS NOT NULL AND [ExternalMessageId] <> ''");
        modelBuilder.Entity<WhatsAppMessageLog>().HasIndex(x => new { x.BusinessId, x.CustomerPhoneNumber });
        modelBuilder.Entity<MarketingLead>().ToTable("ChatbotLeads");
        modelBuilder.Entity<MarketingLead>().HasIndex(x => x.CreatedAtUtc);
        modelBuilder.Entity<MarketingLead>().HasIndex(x => x.WhatsAppNumber);
        // Prevent duplicate active registrations at the database layer as well as in the service.
        // Blank numbers and already-converted leads remain outside this unique constraint.
        modelBuilder.Entity<MarketingLead>()
            .HasIndex(x => x.WhatsAppNumber)
            .IsUnique()
            .HasFilter("[WhatsAppNumber] IS NOT NULL AND [WhatsAppNumber] <> '' AND [Status] = 'Interested' AND [ConvertedBusinessId] IS NULL");
        modelBuilder.Entity<MarketingLead>().HasIndex(x => x.Status);
        modelBuilder.Entity<PlatformVisit>().HasIndex(x => x.CreatedAtUtc);
        modelBuilder.Entity<PlatformVisit>().HasIndex(x => x.VisitorId);
        modelBuilder.Entity<BusinessFollowUp>().HasIndex(x => new { x.BusinessId, x.Status, x.DueAtUtc });
        modelBuilder.Entity<BusinessFollowUp>().HasIndex(x => x.EnquiryId);
        modelBuilder.Entity<BusinessFollowUp>().HasOne<BusinessEnquiry>().WithMany().HasForeignKey(x => x.EnquiryId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<BusinessFollowUp>().HasOne<BusinessCatalogItem>().WithMany().HasForeignKey(x => x.CatalogItemId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<BusinessFollowUp>().HasOne<Business>().WithMany().HasForeignKey(x => x.BusinessId).OnDelete(DeleteBehavior.NoAction);
    }
}
