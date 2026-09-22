using System.ComponentModel.DataAnnotations;

namespace UplivaResortBooking.Models;

public class BusinessCatalogItem
{
    public int Id { get; set; }
    public int BusinessId { get; set; }

    [Required, MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string Description { get; set; } = string.Empty;

    [MaxLength(100)]
    public string PriceText { get; set; } = string.Empty;

    [MaxLength(500)]
    public string ImageUrl { get; set; } = string.Empty;

    [MaxLength(80)]
    public string Category { get; set; } = string.Empty;

    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}
