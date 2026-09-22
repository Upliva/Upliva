using System.ComponentModel.DataAnnotations;

namespace UplivaResortBooking.Models;

public class BusinessOfferViewModel
{
    public int BusinessId { get; set; }
    [Required, MaxLength(180)] public string Title { get; set; } = string.Empty;
    [MaxLength(1000)] public string Description { get; set; } = string.Empty;
    [MaxLength(500)] public string ImageUrl { get; set; } = string.Empty;
    [MaxLength(80)] public string DiscountText { get; set; } = string.Empty;
}
