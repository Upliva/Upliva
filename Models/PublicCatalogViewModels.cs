using System.ComponentModel.DataAnnotations;

namespace UplivaAI.Models;

public class PublicCatalogViewModel
{
    public Business Business { get; set; } = new();
    public IReadOnlyList<BusinessCatalogItem> Products { get; set; } = [];
    public IReadOnlyList<string> Categories { get; set; } = [];
    public string Search { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public IReadOnlyList<BusinessOffer> Offers { get; set; } = [];
}

public class PublicEnquiryViewModel
{
    [Required, MaxLength(150)] public string Name { get; set; } = string.Empty;
    [Required, MaxLength(30)] public string PhoneNumber { get; set; } = string.Empty;
    [EmailAddress, MaxLength(200)] public string? Email { get; set; }
    [Required, MaxLength(2000)] public string Message { get; set; } = string.Empty;
    public int? CatalogItemId { get; set; }
    public string Slug { get; set; } = string.Empty;
    [MaxLength(200)] public string? Website { get; set; }
}
