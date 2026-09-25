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
        var normalizedPhone = BusinessInputRules.NormalizeWhatsApp(whatsappNumber);

        if (!BusinessInputRules.IsOptionalWhatsAppValid(whatsappNumber))
            throw new ArgumentException("If you provide WhatsApp, enter a valid 10-digit Indian number.");

        name = BusinessInputRules.Clean(name);
        businessType = BusinessInputRules.Clean(businessType);

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Business name is required.");
        if (string.IsNullOrWhiteSpace(businessType))
            throw new ArgumentException("Business type is required.");
        if (string.IsNullOrWhiteSpace(normalizedPhone))
            throw new ArgumentException("WhatsApp number is required.");

        // Registration requires the three business identity fields. When WhatsApp is supplied,
        // however, it must never create a duplicate active lead or business.
        if (!string.IsNullOrWhiteSpace(normalizedPhone))
        {
            var phoneVariants = BusinessInputRules.GetWhatsAppVariants(normalizedPhone);
            var duplicate = await db.ChatbotLeads
                .AsNoTracking()
                .AnyAsync(x => phoneVariants.Contains(x.WhatsAppNumber), cancellationToken);

            if (duplicate)
                throw new ArgumentException("This WhatsApp number is already registered. A WhatsApp number can belong to only one business.");

            var existingBusiness = await db.Businesses
                .AsNoTracking()
                .AnyAsync(x => phoneVariants.Contains(x.WhatsAppNumber) && !string.IsNullOrWhiteSpace(x.WhatsAppNumber), cancellationToken);

            if (existingBusiness)
                throw new ArgumentException("This WhatsApp number is already linked to an Upliva business.");
        }

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
        await HydrateBusinessDisplayNamesAsync(recentLeads, cancellationToken);

        return new MarketingLeadStatsViewModel
        {
            TotalBusinesses = await db.Businesses.CountAsync(cancellationToken),
            ActiveBusinesses = await db.Businesses.CountAsync(x => x.Status == BusinessStatuses.Approved, cancellationToken),
            TotalLeads = await db.ChatbotLeads.LongCountAsync(cancellationToken),
            LeadsToday = await db.ChatbotLeads.LongCountAsync(x => x.CreatedAtUtc >= today, cancellationToken),
            InterestedLeads = await db.ChatbotLeads.LongCountAsync(x => x.Status == MarketingLeadStatuses.Interested, cancellationToken),
            WhatsAppSmsLeads = await db.ChatbotLeads.LongCountAsync(x => x.SelectedPlan == MarketingLeadPlans.WhatsAppSms, cancellationToken),
            WhatsAppEnquiryFollowUpLeads = await db.ChatbotLeads.LongCountAsync(x => x.SelectedPlan == MarketingLeadPlans.WhatsAppEnquiryFollowUp, cancellationToken),
            TotalVisits = await db.PlatformVisits.LongCountAsync(cancellationToken),
            UniqueVisitors = await db.PlatformVisits.Select(x => x.VisitorId).Distinct().LongCountAsync(cancellationToken),
            RecentLeads = recentLeads,
            SearchTerm = searchTerm,
            Page = page,
            PageSize = pageSize,
            TotalFilteredLeads = totalFiltered
        };
    }

    public async Task<List<MarketingLead>> GetRecentLeadsAsync(int take = 100, CancellationToken cancellationToken = default)
    {
        var leads = await db.ChatbotLeads.AsNoTracking()
            .OrderByDescending(x => x.CreatedAtUtc)
            .Take(Math.Clamp(take, 1, 5000))
            .ToListAsync(cancellationToken);
        await HydrateBusinessDisplayNamesAsync(leads, cancellationToken);
        return leads;
    }

    public async Task<List<MarketingLead>> GetInterestedLeadsAsync(CancellationToken cancellationToken = default)
    {
        var leads = await db.ChatbotLeads.AsNoTracking()
            // Keep converted Interested leads visible on the first dashboard.
            // The row becomes the entry point to WhatsApp Business and Catalog
            // after the real Business record has been created.
            .Where(x => x.Status == MarketingLeadStatuses.Interested)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);
        await HydrateBusinessDisplayNamesAsync(leads, cancellationToken);
        return leads;
    }

    private async Task HydrateBusinessDisplayNamesAsync(List<MarketingLead> leads, CancellationToken cancellationToken)
    {
        var ids = leads.Where(x => x.ConvertedBusinessId.HasValue)
            .Select(x => x.ConvertedBusinessId!.Value)
            .Distinct()
            .ToList();
        if (ids.Count == 0) return;

        var names = await db.Businesses.AsNoTracking()
            .Where(x => ids.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, x => x.Name, cancellationToken);

        foreach (var lead in leads)
        {
            lead.DisplayName = lead.ConvertedBusinessId.HasValue &&
                               names.TryGetValue(lead.ConvertedBusinessId.Value, out var name)
                ? name
                : lead.Name;
        }
    }

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
