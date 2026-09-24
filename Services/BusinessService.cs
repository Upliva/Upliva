using Microsoft.EntityFrameworkCore;
using UplivaAI.Data;
using UplivaAI.Models;

namespace UplivaAI.Services;

public class BusinessService(
    UplivaDbContext db,
    IAuditLogService auditLogService,
    IBusinessCacheService businessCache) : IBusinessService
{
    public Task<Business?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default) =>
        db.Businesses.AsNoTracking().FirstOrDefaultAsync(x => x.Slug == slug && x.IsPublished && x.Status == BusinessStatuses.Approved, cancellationToken);

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
            GraphApiVersion = "v26.0",
            FeaturedProductLimit = 6,
            IsEnabled = false
        });
        await db.SaveChangesAsync(cancellationToken);
        await auditLogService.WriteAsync(
            "WhatsAppBusinessCreated",
            "BusinessWhatsAppSettings",
            businessId.ToString(),
            businessId,
            "{\"status\":\"created\"}",
            cancellationToken);
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

            if (!MarketingLeadPlans.All.Contains(lead.SelectedPlan, StringComparer.Ordinal))
                throw new InvalidOperationException("Select a Upliva plan for the lead before creating the business.");

            businessName = businessName.Trim();
            if (businessName.Length is < 2 or > 180)
                throw new InvalidOperationException("Enter a valid business name.");

            var duplicatePhone = await db.Businesses.AnyAsync(
                x => x.WhatsAppNumber == lead.WhatsAppNumber && !string.IsNullOrWhiteSpace(x.WhatsAppNumber), cancellationToken);
            if (duplicatePhone)
                throw new InvalidOperationException("A business already exists with this WhatsApp number.");

            var slug = await CreateUniqueSlugAsync(businessName, cancellationToken);
            var business = new Business
            {
                Name = businessName,
                Slug = slug,
                BusinessType = lead.BusinessType.Trim(),
                ServicePlan = lead.SelectedPlan,
                // Lead qualification is the approval gate for this workflow.
                // Once the team has marked the lead Interested and selected a plan,
                // creating the business makes it operational immediately.
                Status = BusinessStatuses.Approved,
                IsPublished = false,
                ApprovedAtUtc = DateTime.UtcNow,
                OwnerName = lead.Name.Trim(),
                PhoneNumber = lead.WhatsAppNumber,
                WhatsAppNumber = lead.WhatsAppNumber,
                Country = "India",
                Tagline = "Grow your business with UplivaAI",
                CreatedAtUtc = DateTime.UtcNow
            };

            db.Businesses.Add(business);
            await db.SaveChangesAsync(cancellationToken);

            if (lead.SelectedPlan is MarketingLeadPlans.WhatsAppWebsite or MarketingLeadPlans.WhatsAppWebsiteEnquiry)
            {
                db.WebsiteConfigurations.Add(new WebsiteConfiguration
                {
                    BusinessId = business.Id,
                    WebsiteTitle = business.Name,
                    MetaDescription = $"{business.Name} · {business.BusinessType}",
                    HeroTitle = business.Name,
                    HeroSubtitle = business.Tagline,
                    AboutTitle = $"About {business.Name}",
                    AboutContent = string.Empty,
                    WhyChooseUsTitle = "Why choose us",
                    WhyChooseUsContent = string.Empty,
                    ServicesTitle = "Our products & services",
                    ServicesContent = string.Empty,
                    CallToActionTitle = "Ready to connect?",
                    CallToActionText = "Contact us on WhatsApp or send an enquiry.",
                    ContactIntro = "Have a question? Send an enquiry and our team can follow up with you.",
                    FooterText = $"{business.Name} · {business.BusinessType}"
                });
            }

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
            System.Text.Json.JsonSerializer.Serialize(new { leadId, plan = createdBusiness.ServicePlan, businessType = createdBusiness.BusinessType }), cancellationToken);

        return createdBusiness;
    }

    public async Task<Business> RegisterAsync(BusinessRegistrationViewModel model, CancellationToken cancellationToken = default)
    {
        var slug = await CreateUniqueSlugAsync(model.BusinessName ?? string.Empty, cancellationToken);

        var business = new Business
        {
            Name = string.IsNullOrWhiteSpace(model.BusinessName) ? "New Business" : model.BusinessName.Trim(),
            Slug = slug,
            BusinessType = string.IsNullOrWhiteSpace(model.BusinessType) ? "Other" : model.BusinessType.Trim(),
            Status = BusinessStatuses.Pending,
            IsPublished = false,
            OwnerName = model.OwnerName.Trim(),
            Email = model.Email?.Trim().ToLowerInvariant() ?? string.Empty,
            PhoneNumber = model.PhoneNumber?.Trim() ?? string.Empty,
            WhatsAppNumber = model.WhatsAppNumber.Trim(),
            Address = model.Address?.Trim() ?? string.Empty,
            City = model.City?.Trim() ?? string.Empty,
            State = model.State?.Trim() ?? string.Empty,
            PostalCode = model.PostalCode?.Trim() ?? string.Empty,
            Country = string.IsNullOrWhiteSpace(model.Country) ? "India" : model.Country.Trim(),
            BusinessHours = model.BusinessHours?.Trim() ?? string.Empty,
            Description = model.Description?.Trim() ?? string.Empty,
            Tagline = "Grow your business with UplivaAI",
            CreatedAtUtc = DateTime.UtcNow
        };

        db.Businesses.Add(business);
        await db.SaveChangesAsync(cancellationToken);

        db.WebsiteConfigurations.Add(new WebsiteConfiguration
        {
            BusinessId = business.Id,
            WebsiteTitle = string.IsNullOrWhiteSpace(model.WebsiteTitle) ? business.Name : model.WebsiteTitle.Trim(),
            MetaDescription = business.Description.Length > 300 ? business.Description[..300] : business.Description,
            HeroTitle = business.Name,
            HeroSubtitle = string.IsNullOrWhiteSpace(business.Description) ? business.Tagline : business.Description,
            AboutTitle = $"About {business.Name}",
            AboutContent = business.Description,
            WhyChooseUsTitle = "Why choose us",
            WhyChooseUsContent = "Tell customers what makes your business different, trusted and worth contacting.",
            ServicesTitle = "Our products & services",
            ServicesContent = "Add the products, services, support, warranty or other customer information that is important to your business.",
            CallToActionTitle = "Ready to connect?",
            CallToActionText = "Contact us on WhatsApp or send an enquiry and our team will help you.",
            ContactIntro = "Have a question? Send an enquiry and our team can follow up with you.",
            FooterText = $"{business.Name} · {business.BusinessType}"
        });
        await db.SaveChangesAsync(cancellationToken);
        return business;
    }

    public async Task ApproveAsync(int id, CancellationToken cancellationToken = default)
    {
        var business = await GetRequiredAsync(id, cancellationToken);
        business.Status = BusinessStatuses.Approved;
        business.ApprovedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        businessCache.InvalidateBusiness(business.Id);
        await auditLogService.WriteAsync(
            AuditActions.BusinessApproved, "Business", business.Id.ToString(), business.Id,
            $"{{\"status\":\"{BusinessStatuses.Approved}\"}}", cancellationToken);
    }

    public async Task RejectAsync(int id, CancellationToken cancellationToken = default)
    {
        var business = await GetRequiredAsync(id, cancellationToken);
        business.Status = BusinessStatuses.Rejected;
        business.IsPublished = false;
        await db.SaveChangesAsync(cancellationToken);
        businessCache.InvalidateBusiness(business.Id);
        await auditLogService.WriteAsync(
            AuditActions.BusinessRejected, "Business", business.Id.ToString(), business.Id,
            $"{{\"status\":\"{BusinessStatuses.Rejected}\"}}", cancellationToken);
    }

    public async Task PublishAsync(int id, CancellationToken cancellationToken = default)
    {
        var business = await GetRequiredAsync(id, cancellationToken);
        if (business.Status != BusinessStatuses.Approved)
            throw new InvalidOperationException("Only an approved business can be published.");

        business.IsPublished = true;
        business.PublishedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        businessCache.InvalidateBusiness(business.Id);
        await auditLogService.WriteAsync(
            AuditActions.BusinessPublished, "Business", business.Id.ToString(), business.Id,
            "{\"isPublished\":true}", cancellationToken);
    }

    public async Task UnpublishAsync(int id, CancellationToken cancellationToken = default)
    {
        var business = await GetRequiredAsync(id, cancellationToken);
        business.IsPublished = false;
        await db.SaveChangesAsync(cancellationToken);
        businessCache.InvalidateBusiness(business.Id);
        await auditLogService.WriteAsync(
            AuditActions.BusinessUnpublished, "Business", business.Id.ToString(), business.Id,
            "{\"isPublished\":false}", cancellationToken);
    }

    private async Task<Business> GetRequiredAsync(int id, CancellationToken cancellationToken) =>
        await db.Businesses.FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
        ?? throw new InvalidOperationException("Business was not found.");

    private async Task<string> CreateUniqueSlugAsync(string name, CancellationToken cancellationToken)
    {
        var baseSlug = new string(name.Trim().ToLowerInvariant()
            .Select(c => char.IsLetterOrDigit(c) ? c : '-')
            .ToArray());

        while (baseSlug.Contains("--"))
            baseSlug = baseSlug.Replace("--", "-");

        baseSlug = baseSlug.Trim('-');
        if (string.IsNullOrWhiteSpace(baseSlug))
            baseSlug = "business";

        var slug = baseSlug;
        var counter = 2;
        while (await db.Businesses.AnyAsync(x => x.Slug == slug, cancellationToken))
            slug = $"{baseSlug}-{counter++}";

        return slug;
    }
}
