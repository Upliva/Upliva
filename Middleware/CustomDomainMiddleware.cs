using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace UplivaAI.Middleware;

public sealed class CustomDomainMiddleware(RequestDelegate next, IServiceScopeFactory scopeFactory, IMemoryCache cache, ILogger<CustomDomainMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        // Only the root path is rewritten. CSS, JS, images and other static files
        // continue through the normal pipeline on the custom domain.
        if ((context.Request.Path == "/" || string.IsNullOrEmpty(context.Request.Path.Value)) &&
            !context.Request.Host.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase) &&
            !context.Request.Host.Host.Equals("127.0.0.1", StringComparison.OrdinalIgnoreCase))
        {
            var host = context.Request.Host.Host.Trim().ToLowerInvariant();
            var cacheKey = $"upliva:custom-domain:{host}";

            var slug = await cache.GetOrCreateAsync<string?>(cacheKey, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
                await using var scope = scopeFactory.CreateAsyncScope();
                var db = scope.ServiceProvider.GetRequiredService<UplivaAI.Data.UplivaDbContext>();
                return await db.Businesses.AsNoTracking()
                    .Where(x => x.IsPublished && x.Status == UplivaAI.Models.BusinessStatuses.Approved &&
                                x.IsCustomDomainEnabled && x.IsCustomDomainVerified &&
                                x.CustomDomain == host)
                    .Select(x => x.Slug)
                    .FirstOrDefaultAsync(context.RequestAborted);
            });

            if (!string.IsNullOrWhiteSpace(slug))
            {
                logger.LogDebug("Custom domain resolved. Host={Host}, Slug={Slug}", host, slug);
                context.Request.Path = $"/business/{slug}";
            }
        }

        await next(context);
    }
}
