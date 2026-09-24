using Microsoft.EntityFrameworkCore;
using UplivaAI.Data;
using UplivaAI.Models;

namespace UplivaAI.Services;

public class MarketingEngagementService(UplivaDbContext db) : IMarketingEngagementService
{
    public Task CaptureLeadAsync(
        string name,
        string businessType,
        string whatsappNumber,
        string visitorId,
        CancellationToken cancellationToken = default) =>
        CaptureLeadAsync(name, businessType, whatsappNumber, visitorId, "UplivaChatbot", cancellationToken);

    public async Task CaptureLeadAsync(
        string name,
        string businessType,
        string whatsappNumber,
        string visitorId,
        string source,
        CancellationToken cancellationToken = default)
    {
        var normalizedPhone = new string((whatsappNumber ?? string.Empty).Where(char.IsDigit).ToArray());

        if (normalizedPhone.Length < 10 || normalizedPhone.Length > 15)
            throw new ArgumentException("Please provide a valid WhatsApp / phone number.");

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Please provide your name.");

        if (string.IsNullOrWhiteSpace(businessType))
            throw new ArgumentException("Please select your business category.");

        // Prevent accidental repeated submissions while an existing lead is still active.
        // A person can still be re-captured after the old lead has been deleted.
        var activeStatuses = new[]
        {
            MarketingLeadStatuses.Interested
        };

        var duplicate = await db.ChatbotLeads
            .AsNoTracking()
            .AnyAsync(x => x.WhatsAppNumber == normalizedPhone
                && activeStatuses.Contains(x.Status)
                && x.ConvertedBusinessId == null, cancellationToken);

        if (duplicate)
            throw new ArgumentException("We already have your details. Our business team will contact you shortly.");

        var existingBusiness = await db.Businesses
            .AsNoTracking()
            .AnyAsync(x => x.WhatsAppNumber == normalizedPhone && !string.IsNullOrWhiteSpace(x.WhatsAppNumber), cancellationToken);

        if (existingBusiness)
            throw new ArgumentException("This WhatsApp number is already linked to an Upliva business.");

        var lead = new MarketingLead
        {
            Name = name.Trim(),
            BusinessType = businessType.Trim(),
            WhatsAppNumber = normalizedPhone,
            VisitorId = visitorId ?? string.Empty,
            Source = string.IsNullOrWhiteSpace(source) ? "UplivaChatbot" : source.Trim(),
            Status = MarketingLeadStatuses.Interested,
            SelectedPlan = string.Empty,
            AdminNotes = string.Empty,
            CreatedAtUtc = DateTime.UtcNow
        };

        db.ChatbotLeads.Add(lead);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task RecordVisitAsync(string visitorId, string path, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(visitorId)) return;

        db.PlatformVisits.Add(new PlatformVisit
        {
            VisitorId = visitorId,
            Path = string.IsNullOrWhiteSpace(path) ? "/" : path[..Math.Min(path.Length, 250)],
            CreatedAtUtc = DateTime.UtcNow
        });

        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<MarketingLeadStatsViewModel> GetStatsAsync(string? searchTerm = null, int page = 1, int pageSize = 10, CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 5, 50);
        searchTerm = searchTerm?.Trim() ?? string.Empty;
        var today = DateTime.UtcNow.Date;

        var filtered = db.ChatbotLeads.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.ToLowerInvariant();
            filtered = filtered.Where(x =>
                x.Name.ToLower().Contains(term) ||
                x.BusinessType.ToLower().Contains(term) ||
                x.WhatsAppNumber.Contains(searchTerm) ||
                x.Status.ToLower().Contains(term) ||
                x.SelectedPlan.ToLower().Contains(term));
        }

        var totalFiltered = await filtered.LongCountAsync(cancellationToken);
        var recentLeads = await filtered
            .OrderByDescending(x => x.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new MarketingLeadStatsViewModel
        {
            TotalBusinesses = await db.Businesses.CountAsync(cancellationToken),
            PublishedBusinesses = await db.Businesses.CountAsync(x => x.IsPublished, cancellationToken),
            TotalLeads = await db.ChatbotLeads.LongCountAsync(cancellationToken),
            LeadsToday = await db.ChatbotLeads.LongCountAsync(x => x.CreatedAtUtc >= today, cancellationToken),
            InterestedLeads = await db.ChatbotLeads.LongCountAsync(x => x.Status == MarketingLeadStatuses.Interested, cancellationToken),
            WhatsAppOnlyLeads = await db.ChatbotLeads.LongCountAsync(x => x.SelectedPlan == MarketingLeadPlans.WhatsAppOnly, cancellationToken),
            WhatsAppWebsiteLeads = await db.ChatbotLeads.LongCountAsync(x => x.SelectedPlan == MarketingLeadPlans.WhatsAppWebsite, cancellationToken),
            WhatsAppWebsiteEnquiryLeads = await db.ChatbotLeads.LongCountAsync(x => x.SelectedPlan == MarketingLeadPlans.WhatsAppWebsiteEnquiry, cancellationToken),
            TotalVisits = await db.PlatformVisits.LongCountAsync(cancellationToken),
            UniqueVisitors = await db.PlatformVisits.Select(x => x.VisitorId).Distinct().LongCountAsync(cancellationToken),
            RecentLeads = recentLeads,
            SearchTerm = searchTerm,
            Page = page,
            PageSize = pageSize,
            TotalFilteredLeads = totalFiltered
        };
    }

    public Task<List<MarketingLead>> GetRecentLeadsAsync(int take = 100, CancellationToken cancellationToken = default) =>
        db.ChatbotLeads.AsNoTracking()
            .OrderByDescending(x => x.CreatedAtUtc)
            .Take(Math.Clamp(take, 1, 5000))
            .ToListAsync(cancellationToken);

    public Task<List<MarketingLead>> GetInterestedLeadsAsync(CancellationToken cancellationToken = default) =>
        db.ChatbotLeads.AsNoTracking()
            // Keep converted Interested leads visible on the first dashboard.
            // The row becomes the entry point to WhatsApp Business and Catalog
            // after the real Business record has been created.
            .Where(x => x.Status == MarketingLeadStatuses.Interested)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);

    public async Task UpdateLeadAsync(
        long leadId,
        string status,
        string? selectedPlan,
        string? adminNotes,
        CancellationToken cancellationToken = default)
    {
        if (!MarketingLeadStatuses.All.Contains(status, StringComparer.Ordinal))
            throw new ArgumentException("Invalid lead status.");

        selectedPlan = selectedPlan?.Trim() ?? string.Empty;
        adminNotes = adminNotes?.Trim() ?? string.Empty;

        if (!string.IsNullOrEmpty(selectedPlan) && !MarketingLeadPlans.All.Contains(selectedPlan, StringComparer.Ordinal))
            throw new ArgumentException("Invalid Upliva plan.");

        // The initial lead pipeline intentionally has only two outcomes.
        // Plan selection is independent and is recorded by the Upliva team after the phone call.
        // A "Not Interested" lead is normally deleted by the team, but if it is retained temporarily
        // its plan is cleared because it is no longer an active prospect.
        if (status == MarketingLeadStatuses.NotInterested)
            selectedPlan = string.Empty;

        if (adminNotes.Length > 1000)
            throw new ArgumentException("Admin notes cannot exceed 1000 characters.");

        var lead = await db.ChatbotLeads.FirstOrDefaultAsync(x => x.Id == leadId, cancellationToken);
        if (lead is null)
            throw new KeyNotFoundException("Lead not found.");

        lead.Status = status;
        lead.SelectedPlan = selectedPlan;
        lead.AdminNotes = adminNotes;

        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteLeadAsync(long leadId, CancellationToken cancellationToken = default)
    {
        var lead = await db.ChatbotLeads.FirstOrDefaultAsync(x => x.Id == leadId, cancellationToken);
        if (lead is null)
            throw new KeyNotFoundException("Lead not found.");

        db.ChatbotLeads.Remove(lead);
        await db.SaveChangesAsync(cancellationToken);
    }
}
