using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using UplivaAI.Middleware;

namespace UplivaAI.Services;

public sealed class AzureBlobStorageService(
    IOptions<AzureStorageOptions> options,
    ILogger<AzureBlobStorageService> logger,
    IIntegrationLogService integrationLogService,
    IHttpContextAccessor httpContextAccessor) : IBlobStorageService
{
    private readonly AzureStorageOptions _options = options.Value;
    private BlobServiceClient? _client;

    private BlobServiceClient GetClient()
    {
        if (_client is not null) return _client;
        if (string.IsNullOrWhiteSpace(_options.ConnectionString))
            throw new InvalidOperationException("Azure Storage is not configured. Set AzureStorage:ConnectionString in User Secrets or environment variables.");
        return _client = new BlobServiceClient(_options.ConnectionString);
    }

    public async Task<BlobUploadResult> UploadBusinessImageAsync(
        int businessId, string area, IFormFile file, CancellationToken cancellationToken = default)
    {
        if (file is null || file.Length == 0)
            throw new ArgumentException("Please select an image file.");

        var maxBytes = Math.Max(1, _options.MaxImageSizeMb) * 1024L * 1024L;
        if (file.Length > maxBytes)
            throw new ArgumentException($"Image size cannot exceed {_options.MaxImageSizeMb} MB.");

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".webp" };
        if (!allowed.Contains(extension))
            throw new ArgumentException("Only JPG, JPEG, PNG and WEBP images are supported.");

        var contentType = file.ContentType?.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(contentType) || !contentType.StartsWith("image/", StringComparison.Ordinal))
            throw new ArgumentException("The selected file must be an image.");

        if (!await HasValidImageSignatureAsync(file, extension, cancellationToken))
            throw new ArgumentException("The selected file is not a valid JPG, PNG or WEBP image.");

        var safeArea = new string((area ?? "catalog").Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(safeArea)) safeArea = "catalog";
        var blobName = $"business/{businessId}/{safeArea}/{Guid.NewGuid():N}{extension}";

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var correlationId = httpContextAccessor.HttpContext?.Items[CorrelationIdMiddleware.ItemKey]?.ToString() ?? "system";
        try
        {
            var container = GetClient().GetBlobContainerClient(_options.ContainerName);
            await container.CreateIfNotExistsAsync(cancellationToken: cancellationToken);
            var blob = container.GetBlobClient(blobName);

            await using var stream = file.OpenReadStream();
            await blob.UploadAsync(stream, new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders { ContentType = contentType }
            }, cancellationToken);

            stopwatch.Stop();
            await integrationLogService.WriteAsync(businessId, "AzureBlob", "UploadImage", "Success", correlationId, stopwatch.ElapsedMilliseconds, cancellationToken: cancellationToken);
            logger.LogInformation("Uploaded business image. BusinessId={BusinessId}, BlobName={BlobName}", businessId, blobName);
            return new BlobUploadResult(blobName, contentType, $"/media/{blobName}");
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            await integrationLogService.WriteAsync(businessId, "AzureBlob", "UploadImage", "Failed", correlationId, stopwatch.ElapsedMilliseconds, errorMessage: ex.Message, cancellationToken: cancellationToken);
            throw;
        }
    }

    private static async Task<bool> HasValidImageSignatureAsync(IFormFile file, string extension, CancellationToken cancellationToken)
    {
        await using var stream = file.OpenReadStream();
        var header = new byte[12];
        var read = await stream.ReadAsync(header.AsMemory(0, header.Length), cancellationToken);
        if (extension is ".jpg" or ".jpeg")
            return read >= 3 && header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF;
        if (extension == ".png")
            return read >= 8 && header.AsSpan(0, 8).SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 });
        if (extension == ".webp")
            return read >= 12 && header.AsSpan(0, 4).SequenceEqual("RIFF"u8) && header.AsSpan(8, 4).SequenceEqual("WEBP"u8);
        return false;
    }

    public async Task DeleteAsync(string? blobName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(blobName)) return;
        var container = GetClient().GetBlobContainerClient(_options.ContainerName);
        var blob = container.GetBlobClient(blobName);
        await blob.DeleteIfExistsAsync(cancellationToken: cancellationToken);
    }

    public async Task<BlobDownloadResult?> DownloadAsync(string blobName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(blobName)) return null;
        var container = GetClient().GetBlobContainerClient(_options.ContainerName);
        var blob = container.GetBlobClient(blobName);
        if (!await blob.ExistsAsync(cancellationToken)) return null;

        var response = await blob.DownloadStreamingAsync(cancellationToken: cancellationToken);
        return new BlobDownloadResult(response.Value.Content, response.Value.Details.ContentType ?? "application/octet-stream");
    }
}
