using System.ComponentModel.DataAnnotations;

namespace UplivaResortBooking.Models;

public class WebsiteConfiguration
{
    public int Id { get; set; }
    public int BusinessId { get; set; }

    [MaxLength(40)]
    public string Theme { get; set; } = "Elegant";

    [MaxLength(20)]
    public string PrimaryColor { get; set; } = "#173b32";

    [MaxLength(20)]
    public string AccentColor { get; set; } = "#c7a26a";

    public bool ShowOffers { get; set; } = true;
    public bool ShowTestimonials { get; set; } = true;
    public bool ShowCatalog { get; set; } = true;
    public bool ShowWhatsApp { get; set; } = true;
}
