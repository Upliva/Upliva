using System.ComponentModel.DataAnnotations;

namespace UplivaAI.Models;

public static class NotificationStatuses
{
    public const string Pending = "Pending";
    public const string Sent = "Sent";
    public const string Failed = "Failed";
}

public class NotificationLog
{
    public long Id { get; set; }
    public int BusinessId { get; set; }
    public int? BusinessEnquiryId { get; set; }

    [Required, MaxLength(40)] public string Channel { get; set; } = "Email";
    [Required, MaxLength(40)] public string Status { get; set; } = NotificationStatuses.Pending;
    [MaxLength(200)] public string Recipient { get; set; } = string.Empty;
    [MaxLength(200)] public string Subject { get; set; } = string.Empty;
    [MaxLength(100)] public string CorrelationId { get; set; } = string.Empty;
    [MaxLength(2000)] public string LastError { get; set; } = string.Empty;
    public int Attempts { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? LastAttemptAtUtc { get; set; }
    public DateTime? NextAttemptAtUtc { get; set; }
}
