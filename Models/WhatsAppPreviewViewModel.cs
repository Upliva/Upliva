namespace UplivaAI.Models;

public class WhatsAppPreviewViewModel
{
    public int BusinessId { get; set; }
    public string BusinessName { get; set; } = string.Empty;
    public string BusinessType { get; set; } = string.Empty;
    public string WhatsAppNumber { get; set; } = string.Empty;
    public string ServicePlan { get; set; } = string.Empty;
    public string Tagline { get; set; } = string.Empty;
    public string LogoUrl { get; set; } = string.Empty;
    public List<WhatsAppPreviewProductViewModel> Products { get; set; } = new();
    public WhatsAppPreviewProductViewModel? FocusProduct { get; set; }
}

public class WhatsAppPreviewProductViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string PriceText { get; set; } = string.Empty;
    public string OriginalPriceText { get; set; } = string.Empty;
    public string DiscountText { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public string ShortDescription { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string StockStatus { get; set; } = string.Empty;
    public decimal? Rating { get; set; }
    public int ReviewCount { get; set; }
    public bool IsSelectedForWhatsApp { get; set; }
    public int Rank { get; set; }
}
