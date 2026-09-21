using Microsoft.EntityFrameworkCore;
using UplivaResortBooking.Models;

namespace UplivaResortBooking.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(ResortDbContext db)
    {
        if (!await db.ResortInfo.AnyAsync())
        {
            db.ResortInfo.Add(new ResortInfo
            {
                Id = 1,
                Name = "Paradise Palm Resort",
                Tagline = "A peaceful escape by the water",
                Phone = "+91 98765 43210",
                WhatsAppNumber = "919876543210",
                Email = "reservations@paradisepalm.in",
                Address = "Beach Road, Goa, India",
                Description = "Relax, recharge and enjoy comfortable rooms, great food and memorable experiences."
            });
        }

        if (!await db.Rooms.AnyAsync())
        {
            db.Rooms.AddRange(
                new Room { Name = "Deluxe Garden Room", Description = "King bed, private balcony and garden view.", PricePerNight = 4500, MaxGuests = 2, ImageUrl = "https://images.unsplash.com/photo-1566665797739-1674de7a421a?auto=format&fit=crop&w=1200&q=80", IsActive = true },
                new Room { Name = "Premium Pool View", Description = "Spacious room with pool-facing balcony and breakfast.", PricePerNight = 6500, MaxGuests = 3, ImageUrl = "https://images.unsplash.com/photo-1582719478250-c89cae4dc85b?auto=format&fit=crop&w=1200&q=80", IsActive = true },
                new Room { Name = "Family Suite", Description = "Two sleeping areas, living space and premium amenities.", PricePerNight = 9000, MaxGuests = 5, ImageUrl = "https://images.unsplash.com/photo-1590490360182-c33d57733427?auto=format&fit=crop&w=1200&q=80", IsActive = true }
            );
        }

        if (!await db.Packages.AnyAsync())
        {
            db.Packages.AddRange(
                new Package { Name = "Weekend Escape", Description = "2 nights with breakfast and pool access.", Price = 12000, DurationNights = 2, ImageUrl = "https://images.unsplash.com/photo-1540541338287-41700207dee6?auto=format&fit=crop&w=1200&q=80", IsActive = true },
                new Package { Name = "Romantic Getaway", Description = "2 nights, dinner for two and a special room setup.", Price = 16500, DurationNights = 2, ImageUrl = "https://images.unsplash.com/photo-1566073771259-6a8506099945?auto=format&fit=crop&w=1200&q=80", IsActive = true },
                new Package { Name = "Family Holiday", Description = "3 nights with breakfast and family activities.", Price = 24000, DurationNights = 3, ImageUrl = "https://images.unsplash.com/photo-1602002418082-a4443e081dd1?auto=format&fit=crop&w=1200&q=80", IsActive = true }
            );
        }

        await db.SaveChangesAsync();
    }
}
