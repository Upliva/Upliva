using System.ComponentModel.DataAnnotations;

namespace UplivaAI.Models;

public class BusinessCatalogItemViewModel
{
    public int Id { get; set; }
    public int BusinessId { get; set; }

    [Required, MaxLength(150)]
    [Display(Name = "Product name")]
    public string Name { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? Brand { get; set; }

    [MaxLength(100)]
    public string? Model { get; set; }

    [MaxLength(80)]
    public string? SKU { get; set; }

    [MaxLength(80)]
    public string? Category { get; set; }

    [MaxLength(100)]
    [Display(Name = "Selling price / price text")]
    public string? PriceText { get; set; }

    [MaxLength(100)]
    [Display(Name = "Original / MRP price")]
    public string? OriginalPriceText { get; set; }

    [MaxLength(80)]
    public string? DiscountText { get; set; }

    [MaxLength(500)]
    [Url]
    [Display(Name = "Product image URL")]
    public string? ImageUrl { get; set; }

    [MaxLength(300)]
    [Display(Name = "Short description")]
    public string? ShortDescription { get; set; }

    [MaxLength(1000)]
    public string? Description { get; set; }

    /// <summary>
    /// Values for the dynamic fields defined by the selected business-type catalog template.
    /// </summary>
    public Dictionary<string, string> CustomAttributes { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    [MaxLength(80)]
    [Display(Name = "Stock / availability")]
    public string? StockStatus { get; set; }

    [Range(0, 5)]
    public decimal? Rating { get; set; }

    [Range(0, int.MaxValue)]
    [Display(Name = "Review count")]
    public int ReviewCount { get; set; }

    [Display(Name = "WhatsApp Featured Product")]
    public bool IsWhatsAppTopPick { get; set; }

    [Range(0, 9999)]
    [Display(Name = "Display order")]
    public int SortOrder { get; set; }
}
