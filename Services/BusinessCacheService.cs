using Microsoft.Extensions.Caching.Memory;
using UplivaAI.Models;

namespace UplivaAI.Services;

public sealed class BusinessCacheService(IMemoryCache cache, ILogger<BusinessCacheService> logger) : IBusinessCacheService
{
    private static string TopPicksKey(int businessId) => $"upliva:whatsapp-top-picks:{businessId}";

    public async Task<List<BusinessCatalogItem>> GetWhatsAppTopPicksAsync(
        int businessId,
        Func<Task<List<BusinessCatalogItem>>> factory,
        CancellationToken cancellationToken = default)
    {
        var key = TopPicksKey(businessId);
        if (cache.TryGetValue(key, out List<BusinessCatalogItem>? cached) && cached is not null)
        {
            logger.LogDebug("WhatsApp catalog cache hit. BusinessId={BusinessId}", businessId);
            return cached;
        }

        var value = await factory();
        cache.Set(key, value, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(2),
            SlidingExpiration = TimeSpan.FromMinutes(1),
            Size = 1
        });
        return value;
    }

    public void InvalidateBusiness(int businessId)
    {
        cache.Remove(TopPicksKey(businessId));
        logger.LogDebug("WhatsApp business cache invalidated. BusinessId={BusinessId}", businessId);
    }
}
