using System.ComponentModel.DataAnnotations;

namespace UplivaAI.Models;

public class BusinessOffer
{
    public int Id { get; set; }
    public int BusinessId { get; set; }

    [Required, MaxLength(180)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string Description { get; set; } = string.Empty;

    [MaxLength(500)]
    public string ImageUrl { get; set; } = string.Empty;

    [MaxLength(80)]
    public string DiscountText { get; set; } = string.Empty;

    public DateTime? StartsOn { get; set; }
    public DateTime? EndsOn { get; set; }
    public bool IsActive { get; set; } = true;
}
