using System.ComponentModel.DataAnnotations;

namespace UplivaAI.Models;

public class CatalogImportViewModel
{
    public int BusinessId { get; set; }
    public string BusinessName { get; set; } = string.Empty;
    public string TemplateDisplayName { get; set; } = string.Empty;
    public string TemplateKey { get; set; } = string.Empty;

    [Required]
    public IFormFile? File { get; set; }

    public string Format { get; set; } = string.Empty;
    public int PreviewRowCount { get; set; }
    public int ImportedRowCount { get; set; }
    public List<CatalogImportPreviewRow> PreviewRows { get; set; } = [];
    public List<string> Errors { get; set; } = [];
}

public class CatalogImportPreviewRow
{
    public int RowNumber { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string PriceText { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public Dictionary<string, string> Values { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

public sealed class CatalogImportRecord
{
    public int RowNumber { get; set; }
    public Dictionary<string, string> Values { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
