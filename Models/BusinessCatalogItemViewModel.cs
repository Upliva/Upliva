using System.ComponentModel.DataAnnotations;

namespace UplivaAI.Models;

public class BusinessCatalogItemViewModel
{
    public int BusinessId { get; set; }

    [Required, MaxLength(150)]
    [Display(Name = "Product name")]
    public string Name { get; set; } = string.Empty;

    [MaxLength(100)]
    public string Brand { get; set; } = string.Empty;

    [MaxLength(100)]
    public string Model { get; set; } = string.Empty;

    [MaxLength(80)]
    public string SKU { get; set; } = string.Empty;

    [Required, MaxLength(80)]
    public string Category { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    [Display(Name = "Selling price")]
    public string PriceText { get; set; } = string.Empty;

    [MaxLength(100)]
    [Display(Name = "Original / MRP price")]
    public string OriginalPriceText { get; set; } = string.Empty;

    [MaxLength(80)]
    public string DiscountText { get; set; } = string.Empty;

    [MaxLength(500)]
    [Url]
    [Display(Name = "Product image URL")]
    public string ImageUrl { get; set; } = string.Empty;

    [MaxLength(300)]
    [Display(Name = "Short description")]
    public string ShortDescription { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string Description { get; set; } = string.Empty;

    [MaxLength(80)]
    [Display(Name = "Stock / availability")]
    public string StockStatus { get; set; } = string.Empty;

    [Range(0, 5)]
    public decimal? Rating { get; set; }

    [Range(0, int.MaxValue)]
    [Display(Name = "Review count")]
    public int ReviewCount { get; set; }

    [Display(Name = "WhatsApp Top 6")]
    public bool IsWhatsAppTopPick { get; set; }

    [Display(Name = "Show on website")]
    public bool ShowOnWebsite { get; set; } = true;

    [Range(0, 9999)]
    [Display(Name = "Display order")]
    public int SortOrder { get; set; }
}
