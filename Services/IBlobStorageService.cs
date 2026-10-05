using Microsoft.AspNetCore.Http;

namespace UplivaAI.Services;

public sealed record BlobUploadResult(string BlobName, string ContentType, string PublicPath);
public sealed record BlobDownloadResult(Stream Content, string ContentType);

/// <summary>
/// Application-level storage abstraction. Controllers and domain services never depend on the Azure SDK.
/// A future S3/local storage implementation can replace this service without changing business logic.
/// </summary>
public interface IBlobStorageService
{
    Task<BlobUploadResult> UploadBusinessImageAsync(int businessId, string area, IFormFile file, CancellationToken cancellationToken = default);
    Task DeleteAsync(string? blobName, CancellationToken cancellationToken = default);
    Task<BlobDownloadResult?> DownloadAsync(string blobName, CancellationToken cancellationToken = default);
}
