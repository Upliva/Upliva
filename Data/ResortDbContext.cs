using Microsoft.EntityFrameworkCore;
using UplivaResortBooking.Models;

namespace UplivaResortBooking.Data;

public class ResortDbContext(DbContextOptions<ResortDbContext> options) : DbContext(options)
{
    public DbSet<Room> Rooms => Set<Room>();
    public DbSet<Package> Packages => Set<Package>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<ResortInfo> ResortInfo => Set<ResortInfo>();

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
