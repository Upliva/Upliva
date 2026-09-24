namespace UplivaAI.Models;

public class MarketingLeadStatsViewModel
{
    public int TotalBusinesses { get; set; }
    public int PublishedBusinesses { get; set; }
    public long TotalLeads { get; set; }
    public long LeadsToday { get; set; }
    public long NewLeads { get; set; }
    public long ContactedLeads { get; set; }
    public long InterestedLeads { get; set; }
    public long ConfirmedLeads { get; set; }
    public long ConvertedLeads { get; set; }
    public long WhatsAppOnlyLeads { get; set; }
    public long WhatsAppWebsiteLeads { get; set; }
    public long WhatsAppWebsiteEnquiryLeads { get; set; }
    public long TotalVisits { get; set; }
    public long UniqueVisitors { get; set; }
    public List<MarketingLead> RecentLeads { get; set; } = [];
    public string SearchTerm { get; set; } = string.Empty;
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public long TotalFilteredLeads { get; set; }
    public int TotalPages => PageSize <= 0 ? 1 : Math.Max(1, (int)Math.Ceiling(TotalFilteredLeads / (double)PageSize));
}
