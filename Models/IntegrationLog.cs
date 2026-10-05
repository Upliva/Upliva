using System.ComponentModel.DataAnnotations;

namespace UplivaAI.Models;

public class IntegrationLog
{
    public long Id { get; set; }
    public int? BusinessId { get; set; }

    [Required, MaxLength(50)] public string IntegrationName { get; set; } = string.Empty;
    [Required, MaxLength(100)] public string Operation { get; set; } = string.Empty;
    [Required, MaxLength(40)] public string Status { get; set; } = string.Empty;
    [MaxLength(100)] public string CorrelationId { get; set; } = string.Empty;
    public int? HttpStatusCode { get; set; }
    public long DurationMs { get; set; }
    public int Attempt { get; set; } = 1;
    [MaxLength(2000)] public string ErrorMessage { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
