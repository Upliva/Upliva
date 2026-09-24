using System.ComponentModel.DataAnnotations;

namespace UplivaAI.Models;

public static class AuditActions
{
    public const string BusinessApproved = "BusinessApproved";
    public const string BusinessRejected = "BusinessRejected";
    public const string BusinessPublished = "BusinessPublished";
    public const string BusinessUnpublished = "BusinessUnpublished";
    public const string BusinessProfileChanged = "BusinessProfileChanged";
    public const string WebsiteContentChanged = "WebsiteContentChanged";
    public const string CatalogAdded = "CatalogAdded";
    public const string CatalogUpdated = "CatalogUpdated";
    public const string WhatsAppShowcaseChanged = "WhatsAppShowcaseChanged";
    public const string CatalogDeleted = "CatalogDeleted";
    public const string OfferAdded = "OfferAdded";
    public const string OfferPublished = "OfferPublished";
}

public class AuditLog
{
    public long Id { get; set; }

    [Required, MaxLength(80)]
    public string Action { get; set; } = string.Empty;

    [MaxLength(80)]
    public string EntityType { get; set; } = string.Empty;

    [MaxLength(100)]
    public string EntityId { get; set; } = string.Empty;

    public int? BusinessId { get; set; }

    public int? UserId { get; set; }

    [MaxLength(200)]
    public string UserEmail { get; set; } = string.Empty;

    [MaxLength(40)]
    public string UserRole { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string CorrelationId { get; set; } = string.Empty;

    [MaxLength(4000)]
    public string Details { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
