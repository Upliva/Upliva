using System.ComponentModel.DataAnnotations;

namespace UplivaAI.Models;

public class CallEvent
{
    public long Id { get; set; }
    public int BusinessId { get; set; }
    public int? CatalogItemId { get; set; }

    [MaxLength(80)] public string Source { get; set; } = "PublicBusinessPage";
    [MaxLength(100)] public string CorrelationId { get; set; } = string.Empty;
    [MaxLength(128)] public string VisitorHash { get; set; } = string.Empty;
    [MaxLength(200)] public string UserAgent { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
