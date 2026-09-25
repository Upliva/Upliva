using UplivaAI.Models;

namespace UplivaAI.Services;

public interface IBusinessService
{
    Task<Business?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<List<Business>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<BusinessWhatsAppSettings?> GetWhatsAppSettingsAsync(int businessId, CancellationToken cancellationToken = default);
    Task CreateWhatsAppSettingsAsync(int businessId, CancellationToken cancellationToken = default);
    Task<Business> CreateFromLeadAsync(long leadId, string businessName, CancellationToken cancellationToken = default);
    Task ApproveAsync(int id, CancellationToken cancellationToken = default);
    Task RejectAsync(int id, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
