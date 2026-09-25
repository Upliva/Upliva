using Microsoft.EntityFrameworkCore;
using UplivaAI.Data;
using UplivaAI.Models;

namespace UplivaAI.Services;

public class BusinessService(
    UplivaDbContext db,
    IAuditLogService auditLogService,
    IBusinessCacheService businessCache,
    ICatalogTemplateService catalogTemplateService) : IBusinessService
{
    public Task<Business?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        db.Businesses.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<List<Business>> GetAllAsync(CancellationToken cancellationToken = default) =>
        db.Businesses.AsNoTracking().OrderByDescending(x => x.CreatedAtUtc).ToListAsync(cancellationToken);

    public Task<BusinessWhatsAppSettings?> GetWhatsAppSettingsAsync(int businessId, CancellationToken cancellationToken = default) =>
        db.BusinessWhatsAppSettings.AsNoTracking().FirstOrDefaultAsync(x => x.BusinessId == businessId, cancellationToken);

    public async Task CreateWhatsAppSettingsAsync(int businessId, CancellationToken cancellationToken = default)
    {
        var business = await GetRequiredAsync(businessId, cancellationToken);
        if (business.Status != BusinessStatuses.Approved)
            throw new InvalidOperationException("Only an approved business can have a WhatsApp Business setup.");

        var exists = await db.BusinessWhatsAppSettings.AnyAsync(x => x.BusinessId == businessId, cancellationToken);
        if (exists) return;

        db.BusinessWhatsAppSettings.Add(new BusinessWhatsAppSettings
        {
            BusinessId = businessId,
            DisplayName = business.Name,
            AboutText = business.Description,
            BusinessCategory = business.BusinessType,
            WelcomeMessage = $"Welcome to {business.Name}! 👋\n\nHow can we help you today?",
            GraphApiVersion = "v26.0",
            FeaturedProductLimit = 6,
            IsEnabled = false
        });

        await db.SaveChangesAsync(cancellationToken);
        await auditLogService.WriteAsync(
            "WhatsAppBusinessCreated", "BusinessWhatsAppSettings", businessId.ToString(), businessId,
            "{\"status\":\"created\"}", cancellationToken);
    }

    public async Task<Business> CreateFromLeadAsync(long leadId, string businessName, CancellationToken cancellationToken = default)
    {
        var strategy = db.Database.CreateExecutionStrategy();
        Business? createdBusiness = null;

        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
            var lead = await db.ChatbotLeads.FirstOrDefaultAsync(x => x.Id == leadId, cancellationToken)
                ?? throw new InvalidOperationException("The selected lead could not be found.");

            if (lead.Status != MarketingLeadStatuses.Interested)
                throw new InvalidOperationException("Only an Interested lead can be converted into a business.");

            if (lead.ConvertedBusinessId.HasValue)
                throw new InvalidOperationException("This lead is already linked to a business.");

            if (!string.IsNullOrWhiteSpace(lead.SelectedPlan) &&
                !MarketingLeadPlans.All.Contains(lead.SelectedPlan, StringComparer.Ordinal))
                throw new InvalidOperationException("Select a valid Upliva plan or leave it blank to use the default.");

            businessName = BusinessInputRules.ResolveBusinessName(businessName, lead.Name);
            var normalizedPhone = BusinessInputRules.NormalizeWhatsApp(lead.WhatsAppNumber);

            if (string.IsNullOrWhiteSpace(businessName) ||
                string.IsNullOrWhiteSpace(lead.BusinessType) ||
                string.IsNullOrWhiteSpace(normalizedPhone))
                throw new InvalidOperationException("Business name, business type and WhatsApp number are required to create a business.");

            if (!string.IsNullOrWhiteSpace(normalizedPhone))
            {
                var phoneVariants = BusinessInputRules.GetWhatsAppVariants(normalizedPhone);
                var duplicatePhone = await db.Businesses.AnyAsync(
                    x => phoneVariants.Contains(x.WhatsAppNumber) && !string.IsNullOrWhiteSpace(x.WhatsAppNumber), cancellationToken);
                if (duplicatePhone)
                    throw new InvalidOperationException("A business already exists with this WhatsApp number.");
            }

            var businessType = BusinessInputRules.ResolveBusinessType(lead.BusinessType);
            var plan = BusinessInputRules.ResolvePlan(lead.SelectedPlan);
            var slug = await CreateUniqueSlugAsync(businessName, cancellationToken);
            var business = new Business
            {
                Name = businessName,
                Slug = slug,
                BusinessType = businessType,
                CatalogTemplateKey = catalogTemplateService.GetTemplate(businessType).Key,
                ServicePlan = plan,
                Status = BusinessStatuses.Approved,
                ApprovedAtUtc = DateTime.UtcNow,
                OwnerName = BusinessInputRules.Clean(lead.Name),
                PhoneNumber = normalizedPhone,
                WhatsAppNumber = normalizedPhone,
                Country = "India",
                Tagline = "Connect with customers on WhatsApp",
                CreatedAtUtc = DateTime.UtcNow
            };

            db.Businesses.Add(business);
            await db.SaveChangesAsync(cancellationToken);

            db.BusinessWhatsAppSettings.Add(new BusinessWhatsAppSettings
            {
                BusinessId = business.Id,
                DisplayName = business.Name,
                AboutText = business.Description,
                BusinessCategory = business.BusinessType,
                WelcomeMessage = $"Welcome to {business.Name}! 👋\n\nHow can we help you today?",
                GraphApiVersion = "v26.0",
                FeaturedProductLimit = 6,
                IsEnabled = false
            });

            lead.Name = business.Name;
            lead.BusinessType = business.BusinessType;
            lead.WhatsAppNumber = business.WhatsAppNumber;
            lead.ConvertedBusinessId = business.Id;
            lead.ConfirmedAtUtc ??= DateTime.UtcNow;
            lead.ConvertedAtUtc = DateTime.UtcNow;

            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            createdBusiness = business;
        });

        if (createdBusiness is null)
            throw new InvalidOperationException("The business could not be created.");

        businessCache.InvalidateBusiness(createdBusiness.Id);
        await auditLogService.WriteAsync(
            "BusinessCreatedFromLead", "Business", createdBusiness.Id.ToString(), createdBusiness.Id,
            System.Text.Json.JsonSerializer.Serialize(new { leadId, plan = createdBusiness.ServicePlan, businessType = createdBusiness.BusinessType, catalogTemplate = createdBusiness.CatalogTemplateKey }), cancellationToken);

        return createdBusiness;
    }

    public async Task ApproveAsync(int id, CancellationToken cancellationToken = default)
    {
        var business = await GetRequiredAsync(id, cancellationToken);
        business.Status = BusinessStatuses.Approved;
        business.ApprovedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        businessCache.InvalidateBusiness(id);
        await auditLogService.WriteAsync(AuditActions.BusinessApproved, "Business", id.ToString(), id,
            $"{{\"status\":\"{BusinessStatuses.Approved}\"}}", cancellationToken);
    }

    public async Task RejectAsync(int id, CancellationToken cancellationToken = default)
    {
        var business = await GetRequiredAsync(id, cancellationToken);
        business.Status = BusinessStatuses.Rejected;
        await db.SaveChangesAsync(cancellationToken);
        businessCache.InvalidateBusiness(id);
        await auditLogService.WriteAsync(AuditActions.BusinessRejected, "Business", id.ToString(), id,
            $"{{\"status\":\"{BusinessStatuses.Rejected}\"}}", cancellationToken);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var strategy = db.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

            var business = await db.Businesses.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
            if (business is null)
                throw new InvalidOperationException("Business was not found.");

            // Keep historical lead rows, but remove their broken business reference.
            var convertedLeads = await db.ChatbotLeads
                .Where(x => x.ConvertedBusinessId == id)
                .ToListAsync(cancellationToken);
            foreach (var lead in convertedLeads)
            {
                lead.ConvertedBusinessId = null;
                lead.ConvertedAtUtc = null;
            }

            // Business owners must not keep a foreign-key reference to a deleted tenant.
            var owners = await db.PlatformUsers
                .Where(x => x.BusinessId == id)
                .ToListAsync(cancellationToken);
            foreach (var owner in owners)
            {
                owner.BusinessId = null;
                owner.IsActive = false;
            }

            // Follow-ups use NO ACTION FKs intentionally, so remove them first.
            var followUps = await db.BusinessFollowUps
                .Where(x => x.BusinessId == id)
                .ToListAsync(cancellationToken);
            db.BusinessFollowUps.RemoveRange(followUps);

            // Child records with cascade FKs are safe to remove explicitly as well.
            var whatsappSettings = await db.BusinessWhatsAppSettings
                .Where(x => x.BusinessId == id)
                .ToListAsync(cancellationToken);
            db.BusinessWhatsAppSettings.RemoveRange(whatsappSettings);

            var catalogItems = await db.BusinessCatalogItems
                .Where(x => x.BusinessId == id)
                .ToListAsync(cancellationToken);
            db.BusinessCatalogItems.RemoveRange(catalogItems);

            var offers = await db.BusinessOffers
                .Where(x => x.BusinessId == id)
                .ToListAsync(cancellationToken);
            db.BusinessOffers.RemoveRange(offers);

            var enquiries = await db.BusinessEnquiries
                .Where(x => x.BusinessId == id)
                .ToListAsync(cancellationToken);
            db.BusinessEnquiries.RemoveRange(enquiries);

            var messages = await db.WhatsAppMessageLogs
                .Where(x => x.BusinessId == id)
                .ToListAsync(cancellationToken);
            db.WhatsAppMessageLogs.RemoveRange(messages);

            // Audit/error rows are retained. Their DB FK is configured to SET NULL.
            await auditLogService.WriteAsync(
                "BusinessDeleted", "Business", id.ToString(), id,
                System.Text.Json.JsonSerializer.Serialize(new { business.Name, business.BusinessType }),
                cancellationToken);

            db.Businesses.Remove(business);
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        });

        businessCache.InvalidateBusiness(id);
    }

    private async Task<Business> GetRequiredAsync(int id, CancellationToken cancellationToken) =>
        await db.Businesses.FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
        ?? throw new InvalidOperationException("Business was not found.");

    private async Task<string> CreateUniqueSlugAsync(string name, CancellationToken cancellationToken)
    {
        var baseSlug = new string(name.Trim().ToLowerInvariant()
            .Select(c => char.IsLetterOrDigit(c) ? c : '-')
            .ToArray());

        while (baseSlug.Contains("--")) baseSlug = baseSlug.Replace("--", "-");
        baseSlug = baseSlug.Trim('-');
        if (string.IsNullOrWhiteSpace(baseSlug)) baseSlug = "business";

        var slug = baseSlug;
        var counter = 2;
        while (await db.Businesses.AnyAsync(x => x.Slug == slug, cancellationToken))
            slug = $"{baseSlug}-{counter++}";
        return slug;
    }
}
