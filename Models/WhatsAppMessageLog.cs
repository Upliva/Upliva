using System.ComponentModel.DataAnnotations;

namespace UplivaAI.Models;

public static class WhatsAppMessageDirections
{
    public const string Inbound = "Inbound";
    public const string Outbound = "Outbound";
}

public class WhatsAppMessageLog
{
    public long Id { get; set; }
    public int BusinessId { get; set; }

    [Required, MaxLength(20)]
    public string Direction { get; set; } = WhatsAppMessageDirections.Inbound;

    [Required, MaxLength(40)]
    public string MessageType { get; set; } = "text";

    [MaxLength(30)]
    public string CustomerPhoneNumber { get; set; } = string.Empty;

    [MaxLength(150)]
    public string CustomerName { get; set; } = string.Empty;

    [MaxLength(200)]
    public string ExternalMessageId { get; set; } = string.Empty;

    [MaxLength(100)]
    public string SelectionId { get; set; } = string.Empty;

    [MaxLength(5000)]
    public string MessageText { get; set; } = string.Empty;

    [MaxLength(40)]
    public string DeliveryStatus { get; set; } = "Received";

    [MaxLength(2000)]
    public string ErrorMessage { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
