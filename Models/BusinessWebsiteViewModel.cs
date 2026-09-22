namespace UplivaResortBooking.Models;

public class BusinessWebsiteViewModel
{
    public Business Business { get; set; } = null!;
    public WebsiteConfiguration Configuration { get; set; } = null!;
    public List<BusinessCatalogItem> Catalog { get; set; } = [];
    public List<BusinessOffer> Offers { get; set; } = [];
    public List<BusinessTestimonial> Testimonials { get; set; } = [];
}
