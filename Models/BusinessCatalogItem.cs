using System.ComponentModel.DataAnnotations;

namespace UplivaAI.Models;

public class BusinessCatalogItem
{
    public int Id { get; set; }
    public int BusinessId { get; set; }

    [Required, MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(100)]
    public string Brand { get; set; } = string.Empty;

    [MaxLength(100)]
    public string Model { get; set; } = string.Empty;

    [MaxLength(80)]
    public string SKU { get; set; } = string.Empty;

    [MaxLength(80)]
    public string Category { get; set; } = string.Empty;

    [MaxLength(100)]
    public string PriceText { get; set; } = string.Empty;

    [MaxLength(100)]
    public string OriginalPriceText { get; set; } = string.Empty;

    [MaxLength(80)]
    public string DiscountText { get; set; } = string.Empty;

    [MaxLength(500)]
    public string ImageUrl { get; set; } = string.Empty;

    [MaxLength(300)]
    public string ShortDescription { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string Description { get; set; } = string.Empty;

    [MaxLength(80)]
    public string StockStatus { get; set; } = string.Empty;

    public decimal? Rating { get; set; }
    public int ReviewCount { get; set; }

    /// <summary>
    /// When true, this product is included in the business's configurable WhatsApp featured showcase.
    /// The same selected products are highlighted first on the public website.
    /// </summary>
    public bool IsWhatsAppTopPick { get; set; }

    /// <summary>Controls visibility in the public business website catalog.</summary>
    public bool ShowOnWebsite { get; set; } = true;

    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}
