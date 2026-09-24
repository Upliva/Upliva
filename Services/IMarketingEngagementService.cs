using UplivaAI.Models;

namespace UplivaAI.Services;

public interface IMarketingEngagementService
{
    Task CaptureLeadAsync(string name, string businessType, string whatsappNumber, string visitorId, CancellationToken cancellationToken = default);
    Task CaptureLeadAsync(string name, string businessType, string whatsappNumber, string visitorId, string source, CancellationToken cancellationToken = default);
    Task RecordVisitAsync(string visitorId, string path, CancellationToken cancellationToken = default);
    Task<MarketingLeadStatsViewModel> GetStatsAsync(string? searchTerm = null, int page = 1, int pageSize = 10, CancellationToken cancellationToken = default);
    Task<List<MarketingLead>> GetRecentLeadsAsync(int take = 100, CancellationToken cancellationToken = default);
    Task<List<MarketingLead>> GetInterestedLeadsAsync(CancellationToken cancellationToken = default);
    Task UpdateLeadAsync(long leadId, string status, string? selectedPlan, string? adminNotes, CancellationToken cancellationToken = default);
    Task DeleteLeadAsync(long leadId, CancellationToken cancellationToken = default);
}
