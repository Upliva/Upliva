using Microsoft.Extensions.Caching.Memory;
using UplivaAI.Models;

namespace UplivaAI.Services;

public sealed class BusinessCacheService(IMemoryCache cache, ILogger<BusinessCacheService> logger) : IBusinessCacheService
{
    private static string WebsiteKey(int businessId) => $"upliva:business-page:{businessId}";
    private static string TopPicksKey(int businessId) => $"upliva:catalog-top-picks:{businessId}";

    public async Task<BusinessWebsiteViewModel> GetOrCreateWebsiteAsync(
        int businessId,
        Func<Task<BusinessWebsiteViewModel>> factory,
        CancellationToken cancellationToken = default)
    {
        var key = WebsiteKey(businessId);
        if (cache.TryGetValue(key, out BusinessWebsiteViewModel? cached) && cached is not null)
        {
            logger.LogDebug("Business website cache hit. BusinessId={BusinessId}", businessId);
            return cached;
        }

        var value = await factory();
        cache.Set(key, value, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5),
            SlidingExpiration = TimeSpan.FromMinutes(2),
            Size = 1
        });

        logger.LogDebug("Business website cache populated. BusinessId={BusinessId}", businessId);
        return value;
    }

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

        logger.LogDebug("WhatsApp catalog cache populated. BusinessId={BusinessId}", businessId);
        return value;
    }

    public void InvalidateBusiness(int businessId)
    {
        cache.Remove(WebsiteKey(businessId));
        cache.Remove(TopPicksKey(businessId));
        logger.LogDebug("Business cache invalidated. BusinessId={BusinessId}", businessId);
    }
}
