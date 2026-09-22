using System.ComponentModel.DataAnnotations;

namespace UplivaResortBooking.Models;

public class BusinessTestimonial
{
    public int Id { get; set; }
    public int BusinessId { get; set; }

    [Required, MaxLength(120)]
    public string CustomerName { get; set; } = string.Empty;

    [Required, MaxLength(1000)]
    public string Feedback { get; set; } = string.Empty;

    [MaxLength(100)]
    public string Location { get; set; } = string.Empty;

    public bool IsDemo { get; set; }
    public bool IsPublished { get; set; }
}
