using System.ComponentModel.DataAnnotations;

namespace UplivaAI.Models;

public class WebsiteContentViewModel
{
    public int BusinessId { get; set; }

    [Required, MaxLength(180), Display(Name = "Browser / website title")]
    public string WebsiteTitle { get; set; } = string.Empty;

    [MaxLength(300), Display(Name = "SEO description")]
    public string MetaDescription { get; set; } = string.Empty;

    [Required, MaxLength(180), Display(Name = "Hero heading")]
    public string HeroTitle { get; set; } = string.Empty;

    [MaxLength(500), Display(Name = "Hero text")]
    public string HeroSubtitle { get; set; } = string.Empty;

    [MaxLength(120), Display(Name = "About heading")]
    public string AboutTitle { get; set; } = "About us";

    [MaxLength(3000), Display(Name = "About content")]
    public string AboutContent { get; set; } = string.Empty;

    [MaxLength(120), Display(Name = "Why choose us heading")]
    public string WhyChooseUsTitle { get; set; } = "Why choose us";

    [MaxLength(3000), Display(Name = "Why choose us content")]
    public string WhyChooseUsContent { get; set; } = string.Empty;

    [MaxLength(120), Display(Name = "Services heading")]
    public string ServicesTitle { get; set; } = "Our services";

    [MaxLength(3000), Display(Name = "Services content")]
    public string ServicesContent { get; set; } = string.Empty;

    [MaxLength(120), Display(Name = "Call-to-action heading")]
    public string CallToActionTitle { get; set; } = "Ready to connect?";

    [MaxLength(1000), Display(Name = "Call-to-action text")]
    public string CallToActionText { get; set; } = string.Empty;

    [MaxLength(500), Display(Name = "Contact introduction")]
    public string ContactIntro { get; set; } = string.Empty;

    [MaxLength(500), Display(Name = "Footer text")]
    public string FooterText { get; set; } = string.Empty;

    public bool ShowWhyChooseUs { get; set; } = true;
    public bool ShowServices { get; set; } = true;
    public bool ShowCallToAction { get; set; } = true;
    public bool ShowOffers { get; set; } = true;
    public bool ShowTestimonials { get; set; } = true;
    public bool ShowCatalog { get; set; } = true;
    public bool ShowWhatsApp { get; set; } = true;
}
