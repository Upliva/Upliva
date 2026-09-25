using UplivaAI.Models;

namespace UplivaAI.Services;

public interface IBusinessCacheService
{
    Task<List<BusinessCatalogItem>> GetWhatsAppTopPicksAsync(
        int businessId,
        Func<Task<List<BusinessCatalogItem>>> factory,
        CancellationToken cancellationToken = default);

    void InvalidateBusiness(int businessId);
}
