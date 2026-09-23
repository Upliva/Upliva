using Microsoft.EntityFrameworkCore;
using UplivaAI.Data;
using UplivaAI.Models;

namespace UplivaAI.Services;

public class BusinessService(UplivaDbContext db) : IBusinessService
{
    public Task<Business?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default) =>
        db.Businesses.AsNoTracking().FirstOrDefaultAsync(x => x.Slug == slug && x.IsPublished && x.Status == BusinessStatuses.Approved, cancellationToken);

    public Task<Business?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        db.Businesses.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<List<Business>> GetAllAsync(CancellationToken cancellationToken = default) =>
        db.Businesses.AsNoTracking().OrderByDescending(x => x.CreatedAtUtc).ToListAsync(cancellationToken);

    public async Task<Business> RegisterAsync(BusinessRegistrationViewModel model, CancellationToken cancellationToken = default)
    {
        var slug = await CreateUniqueSlugAsync(model.BusinessName, cancellationToken);

        var business = new Business
        {
            Name = model.BusinessName.Trim(),
            Slug = slug,
            BusinessType = model.BusinessType.Trim(),
            Status = BusinessStatuses.Pending,
            IsPublished = false,
            OwnerName = model.OwnerName.Trim(),
            Email = model.Email.Trim().ToLowerInvariant(),
            PhoneNumber = model.PhoneNumber.Trim(),
            WhatsAppNumber = model.WhatsAppNumber.Trim(),
            Address = model.Address.Trim(),
            City = model.City.Trim(),
            State = model.State.Trim(),
            PostalCode = model.PostalCode.Trim(),
            Country = string.IsNullOrWhiteSpace(model.Country) ? "India" : model.Country.Trim(),
            BusinessHours = model.BusinessHours.Trim(),
            Description = model.Description.Trim(),
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
    }

    public async Task RejectAsync(int id, CancellationToken cancellationToken = default)
    {
        var business = await GetRequiredAsync(id, cancellationToken);
        business.Status = BusinessStatuses.Rejected;
        business.IsPublished = false;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task PublishAsync(int id, CancellationToken cancellationToken = default)
    {
        var business = await GetRequiredAsync(id, cancellationToken);
        if (business.Status != BusinessStatuses.Approved)
            throw new InvalidOperationException("Only an approved business can be published.");

        business.IsPublished = true;
        business.PublishedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task UnpublishAsync(int id, CancellationToken cancellationToken = default)
    {
        var business = await GetRequiredAsync(id, cancellationToken);
        business.IsPublished = false;
        await db.SaveChangesAsync(cancellationToken);
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
