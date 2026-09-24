namespace UplivaAI.Models;

/// <summary>
/// First-level admin dashboard for the sales-to-WhatsApp workflow.
/// Interested leads stay visible after conversion so the same row can be
/// used to open WhatsApp setup or the catalog for the created business.
/// </summary>
public class AdminBusinessListViewModel
{
    public List<MarketingLead> InterestedLeads { get; set; } = [];
    public List<Business> Businesses { get; set; } = [];
    public int? SelectedBusinessId { get; set; }

    public IEnumerable<MarketingLead> WhatsAppLeads => FilterByBusiness(InterestedLeads.Where(x => x.SelectedPlan == MarketingLeadPlans.WhatsAppOnly));
    public IEnumerable<MarketingLead> WhatsAppWebsiteLeads => FilterByBusiness(InterestedLeads.Where(x => x.SelectedPlan == MarketingLeadPlans.WhatsAppWebsite));
    public IEnumerable<MarketingLead> WhatsAppWebsiteEnquiryLeads => FilterByBusiness(InterestedLeads.Where(x => x.SelectedPlan == MarketingLeadPlans.WhatsAppWebsiteEnquiry));
    public IEnumerable<MarketingLead> UnassignedInterestedLeads => FilterByBusiness(InterestedLeads.Where(x => string.IsNullOrWhiteSpace(x.SelectedPlan)));

    private IEnumerable<MarketingLead> FilterByBusiness(IEnumerable<MarketingLead> leads)
    {
        if (!SelectedBusinessId.HasValue)
            return leads;

        return leads.Where(x => x.ConvertedBusinessId == SelectedBusinessId.Value);
    }
}
