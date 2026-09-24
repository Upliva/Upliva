namespace UplivaAI.Services;

public sealed record BusinessBrochure(
    string FileName,
    string DisplayName,
    string RelativeUrl,
    long Length,
    DateTime LastModifiedUtc);

/// <summary>
/// Business brochure/document storage abstraction.
/// The MVP uses local wwwroot storage. A future Azure Blob implementation can
/// replace this service without changing controllers, views or business flows.
/// </summary>
public interface IBusinessBrochureService
{
    Task<IReadOnlyList<BusinessBrochure>> GetAsync(int businessId, CancellationToken cancellationToken = default);
    Task<BusinessBrochure> SaveAsync(int businessId, IFormFile file, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int businessId, string fileName, CancellationToken cancellationToken = default);
}
