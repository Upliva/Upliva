using System.ComponentModel.DataAnnotations;

namespace UplivaAI.Models;

public class ErrorLog
{
    public long Id { get; set; }

    [Required, MaxLength(100)]
    public string CorrelationId { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string ExceptionType { get; set; } = string.Empty;

    [Required, MaxLength(2000)]
    public string Message { get; set; } = string.Empty;

    [MaxLength(10000)]
    public string StackTrace { get; set; } = string.Empty;

    [MaxLength(10)]
    public string HttpMethod { get; set; } = string.Empty;

    [MaxLength(500)]
    public string RequestPath { get; set; } = string.Empty;

    [MaxLength(200)]
    public string UserEmail { get; set; } = string.Empty;

    public int? UserId { get; set; }
    public int? BusinessId { get; set; }

    public int StatusCode { get; set; } = 500;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
