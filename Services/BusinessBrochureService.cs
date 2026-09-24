using System.Text;

namespace UplivaAI.Services;

/// <summary>
/// Initial MVP implementation: stores PDF brochures under wwwroot.
/// Keep this behind IBusinessBrochureService so Azure Blob Storage can be
/// introduced later without changing the application workflow.
/// </summary>
public sealed class BusinessBrochureService(IWebHostEnvironment environment) : IBusinessBrochureService
{
    private const long MaxFileSize = 20 * 1024 * 1024;
    private static readonly string[] AllowedExtensions = [".pdf"];
    private const string RelativeRoot = "uploads/business";

    private string GetWebRoot()
        => string.IsNullOrWhiteSpace(environment.WebRootPath)
            ? Path.Combine(environment.ContentRootPath, "wwwroot")
            : environment.WebRootPath;

    private string GetFolder(int businessId)
        => Path.Combine(GetWebRoot(), "uploads", "business", businessId.ToString(), "brochures");

    public Task<IReadOnlyList<BusinessBrochure>> GetAsync(
        int businessId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var folder = GetFolder(businessId);
        if (!Directory.Exists(folder))
            return Task.FromResult<IReadOnlyList<BusinessBrochure>>([]);

        var files = Directory.EnumerateFiles(folder, "*.pdf", SearchOption.TopDirectoryOnly)
            .Select(path => new FileInfo(path))
            .OrderByDescending(x => x.LastWriteTimeUtc)
            .Select(x => new BusinessBrochure(
                x.Name,
                Path.GetFileNameWithoutExtension(x.Name).Replace('_', ' '),
                $"/{RelativeRoot}/{businessId}/brochures/{Uri.EscapeDataString(x.Name)}",
                x.Length,
                x.LastWriteTimeUtc))
            .ToList();

        return Task.FromResult<IReadOnlyList<BusinessBrochure>>(files);
    }

    public async Task<BusinessBrochure> SaveAsync(
        int businessId,
        IFormFile file,
        CancellationToken cancellationToken = default)
    {
        if (businessId <= 0)
            throw new InvalidOperationException("A valid business is required.");

        if (file is null || file.Length == 0)
            throw new InvalidOperationException("Please choose a PDF brochure.");

        if (file.Length > MaxFileSize)
            throw new InvalidOperationException("Brochure must be 20 MB or smaller.");

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(extension, StringComparer.Ordinal))
            throw new InvalidOperationException("Only PDF brochures are allowed.");

        // Extension alone is not trusted. Check the PDF signature before storing it.
        await using var input = file.OpenReadStream();
        var header = new byte[5];
        var read = await input.ReadAsync(header.AsMemory(0, header.Length), cancellationToken);
        if (read != 5 || Encoding.ASCII.GetString(header) != "%PDF-")
            throw new InvalidOperationException("The uploaded file is not a valid PDF.");

        var folder = GetFolder(businessId);
        Directory.CreateDirectory(folder);

        var safeBaseName = SanitizeBaseName(Path.GetFileNameWithoutExtension(file.FileName));
        var fileName = $"{safeBaseName}_{Guid.NewGuid():N}.pdf";
        var destination = Path.Combine(folder, fileName);

        // Rewind because the signature check consumed the first five bytes.
        if (input.CanSeek)
            input.Position = 0;
        else
            throw new InvalidOperationException("The uploaded PDF stream cannot be processed safely.");

        await using (var output = new FileStream(
            destination,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None,
            64 * 1024,
            FileOptions.Asynchronous | FileOptions.SequentialScan))
        {
            await input.CopyToAsync(output, cancellationToken);
        }

        var info = new FileInfo(destination);
        return new BusinessBrochure(
            info.Name,
            safeBaseName.Replace('_', ' '),
            $"/{RelativeRoot}/{businessId}/brochures/{Uri.EscapeDataString(info.Name)}",
            info.Length,
            info.LastWriteTimeUtc);
    }

    public Task<bool> DeleteAsync(
        int businessId,
        string fileName,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (businessId <= 0 || string.IsNullOrWhiteSpace(fileName))
            return Task.FromResult(false);

        // Prevent path traversal; only a generated file name can be deleted.
        var safeName = Path.GetFileName(fileName);
        if (!string.Equals(safeName, fileName, StringComparison.Ordinal) ||
            !safeName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
            return Task.FromResult(false);

        var path = Path.Combine(GetFolder(businessId), safeName);
        if (!File.Exists(path))
            return Task.FromResult(false);

        File.Delete(path);
        return Task.FromResult(true);
    }

    private static string SanitizeBaseName(string value)
    {
        var cleaned = value;
        foreach (var invalid in Path.GetInvalidFileNameChars())
            cleaned = cleaned.Replace(invalid, '_');

        cleaned = string.Join('_', cleaned
            .Split(new[] { ' ', '.', '-', '_' }, StringSplitOptions.RemoveEmptyEntries)
            .Take(12));

        return string.IsNullOrWhiteSpace(cleaned) ? "brochure" : cleaned;
    }
}
