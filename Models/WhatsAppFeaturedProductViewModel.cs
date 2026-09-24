namespace UplivaAI.Models;

public class WhatsAppFeaturedProductViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string SKU { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string PriceText { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public bool IsSelected { get; set; }
    public bool IsActive { get; set; }
    public int Rank { get; set; }
}
