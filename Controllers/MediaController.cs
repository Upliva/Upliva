using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UplivaAI.Data;
using UplivaAI.Services;

namespace UplivaAI.Controllers;

/// <summary>
/// Publicly serves only business profile/catalog/banner media. Private documents are not exposed here.
/// </summary>
public class MediaController(IBlobStorageService storage, UplivaDbContext db) : Controller
{
    [HttpGet("/media/{*blobName}")]
    [ResponseCache(Duration = 86400, Location = ResponseCacheLocation.Any, NoStore = false)]
    public async Task<IActionResult> Get(string blobName, CancellationToken cancellationToken)
    {
        if (!TryParsePublicBusinessBlob(blobName, out var businessId, out var area))
            return NotFound();

        if (!await db.Businesses.AsNoTracking().AnyAsync(x => x.Id == businessId && x.Status == Models.BusinessStatuses.Approved, cancellationToken))
            return NotFound();

        if (area is not ("catalog" or "profile" or "banner"))
            return NotFound();

        var file = await storage.DownloadAsync(blobName, cancellationToken);
        if (file is null) return NotFound();
        return File(file.Content, file.ContentType, enableRangeProcessing: true);
    }

    private static bool TryParsePublicBusinessBlob(string blobName, out int businessId, out string area)
    {
        businessId = 0;
        area = string.Empty;
        var parts = blobName.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 4 || !string.Equals(parts[0], "business", StringComparison.OrdinalIgnoreCase)) return false;
        if (!int.TryParse(parts[1], out businessId) || businessId <= 0) return false;
        area = parts[2].ToLowerInvariant();
        return !string.IsNullOrWhiteSpace(parts[3]);
    }
}
