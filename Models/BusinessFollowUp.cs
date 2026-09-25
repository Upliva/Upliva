using System.ComponentModel.DataAnnotations;

namespace UplivaAI.Models;

public static class FollowUpStatuses
{
    public const string Pending = "Pending";
    public const string InProgress = "In Progress";
    public const string Completed = "Completed";
    public const string Cancelled = "Cancelled";

    public static IReadOnlyList<string> All { get; } = new[] { Pending, InProgress, Completed, Cancelled };
}

public class BusinessFollowUp
{
    public long Id { get; set; }
    public int BusinessId { get; set; }
    public int? EnquiryId { get; set; }
    public int? CatalogItemId { get; set; }

    [Required, MaxLength(150)]
    public string CustomerName { get; set; } = string.Empty;

    [Required, MaxLength(30)]
    public string CustomerPhoneNumber { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string Notes { get; set; } = string.Empty;

    [Required, MaxLength(40)]
    public string Status { get; set; } = FollowUpStatuses.Pending;

    public DateTime? DueAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
