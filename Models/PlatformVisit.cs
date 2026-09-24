using System.ComponentModel.DataAnnotations;

namespace UplivaAI.Models;

/// <summary>
/// Lightweight public marketing-site visit tracking. VisitorId is an opaque random
/// cookie value; no IP address is stored.
/// </summary>
public class PlatformVisit
{
    public long Id { get; set; }

    [Required, MaxLength(100)]
    public string VisitorId { get; set; } = string.Empty;

    [Required, MaxLength(250)]
    public string Path { get; set; } = "/";

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
