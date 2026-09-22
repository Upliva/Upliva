using Microsoft.EntityFrameworkCore;
using UplivaResortBooking.Models;

namespace UplivaResortBooking.Data;

public class ResortDbContext(DbContextOptions<ResortDbContext> options) : DbContext(options)
{
    public DbSet<Room> Rooms => Set<Room>();
    public DbSet<Package> Packages => Set<Package>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<ResortInfo> ResortInfo => Set<ResortInfo>();

    public DbSet<PlatformUser> PlatformUsers => Set<PlatformUser>();
    public DbSet<Business> Businesses => Set<Business>();
    public DbSet<BusinessCatalogItem> BusinessCatalogItems => Set<BusinessCatalogItem>();
    public DbSet<BusinessOffer> BusinessOffers => Set<BusinessOffer>();
    public DbSet<BusinessTestimonial> BusinessTestimonials => Set<BusinessTestimonial>();
    public DbSet<WebsiteConfiguration> WebsiteConfigurations => Set<WebsiteConfiguration>();
    public DbSet<BusinessEnquiry> BusinessEnquiries => Set<BusinessEnquiry>();
    public DbSet<BusinessWhatsAppSettings> BusinessWhatsAppSettings => Set<BusinessWhatsAppSettings>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Room>()
            .Property(x => x.PricePerNight)
            .HasPrecision(18, 2);

        modelBuilder.Entity<Package>()
            .Property(x => x.Price)
            .HasPrecision(18, 2);

        modelBuilder.Entity<Booking>()
            .Property(x => x.TotalAmount)
            .HasPrecision(18, 2);

        modelBuilder.Entity<Booking>()
            .HasIndex(x => x.BookingReference)
            .IsUnique();

        modelBuilder.Entity<Booking>()
            .HasIndex(x => new { x.RoomId, x.CheckIn, x.CheckOut });

        modelBuilder.Entity<ResortInfo>()
            .Property(x => x.Id)
            .ValueGeneratedNever();
    }
}
