using Microsoft.EntityFrameworkCore;
using GloHorizonApi.Models.DomainModels;
using BCrypt.Net;

namespace GloHorizonApi.Data;

public static class DatabaseSeeder
{
    public static async Task SeedAdminAsync(ApplicationDbContext context)
    {
        // Check if any SuperAdmin exists
        var existingSuperAdmin = await context.Admins
            .FirstOrDefaultAsync(a => a.Role == AdminRole.SuperAdmin);

        if (existingSuperAdmin == null)
        {
            // Create default SuperAdmin
            var superAdmin = new Admin
            {
                FullName = "Super Admin",
                Email = "admin@glohorizon.com",
                PhoneNumber = "+233123456789",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!"),
                Role = AdminRole.SuperAdmin,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            context.Admins.Add(superAdmin);
            await context.SaveChangesAsync();
        }
        else
        {
            Console.WriteLine("SuperAdmin already exists.");
        }
    }

    public static async Task SeedTravelPackagesAsync(ApplicationDbContext context)
    {
        // Check if any travel packages exist
        if (!await context.TravelPackages.AnyAsync())
        {
            var packages = new List<TravelPackage>
            {
                new TravelPackage
                {
                    Name = "Dubai Desert Safari & City Tour",
                    Description = "Experience the best of Dubai with our comprehensive 5-day package including desert safari, city tours, and luxury accommodations.",
                    Destination = "Dubai, UAE",
                    Duration = 5,
                    BasePrice = 2500.00m,
                    Currency = "USD",
                    Category = PackageCategory.Luxury,
                    IsFeatured = true,
                    IsActive = true,
                    ImageUrl = "https://images.unsplash.com/photo-1512453979798-5ea266f8880c?w=800",
                    PackageDetails = "{\"inclusions\":[\"Round-trip flights\",\"4-star hotel accommodation\",\"Airport transfers\",\"Desert safari with BBQ dinner\",\"Dubai city tour\",\"Burj Khalifa tickets\"],\"exclusions\":[\"Lunch and some dinners\",\"Personal shopping\",\"Travel insurance\"]}",
                    CreatedAt = DateTime.UtcNow
                },
                new TravelPackage
                {
                    Name = "London Heritage & Culture Experience",
                    Description = "Discover the rich history and vibrant culture of London with guided tours, museum visits, and traditional experiences.",
                    Destination = "London, UK",
                    Duration = 7,
                    BasePrice = 3200.00m,
                    Currency = "USD",
                    Category = PackageCategory.Cultural,
                    IsFeatured = true,
                    IsActive = true,
                    ImageUrl = "https://images.unsplash.com/photo-1513635269975-59663e0ac1ad?w=800",
                    PackageDetails = "{\"inclusions\":[\"Round-trip flights\",\"3-star central hotel\",\"7-day transport pass\",\"Museum passes\",\"Royal tour experiences\",\"West End show tickets\"],\"exclusions\":[\"Most meals\",\"Personal expenses\",\"Private transportation\"]}",
                    CreatedAt = DateTime.UtcNow
                },
                new TravelPackage
                {
                    Name = "Cape Town Adventure Package",
                    Description = "Explore the stunning landscapes of Cape Town with wine tours, Table Mountain adventures, and coastal excursions.",
                    Destination = "Cape Town, South Africa",
                    Duration = 6,
                    BasePrice = 1800.00m,
                    Currency = "USD",
                    Category = PackageCategory.Adventure,
                    IsFeatured = true,
                    IsActive = true,
                    ImageUrl = "https://images.unsplash.com/photo-1580060839134-75a5edca2e99?w=800",
                    PackageDetails = "{\"inclusions\":[\"Round-trip flights\",\"Boutique hotel stay\",\"Car rental included\",\"Wine tasting tours\",\"Table Mountain cable car\",\"Penguin colony visit\"],\"exclusions\":[\"Some meals\",\"Fuel costs\",\"Optional activities\"]}",
                    CreatedAt = DateTime.UtcNow
                },
                new TravelPackage
                {
                    Name = "Tokyo Culture & Cuisine Journey",
                    Description = "Immerse yourself in Japanese culture with traditional experiences, modern attractions, and culinary adventures.",
                    Destination = "Tokyo, Japan",
                    Duration = 8,
                    BasePrice = 4200.00m,
                    Currency = "USD",
                    Category = PackageCategory.Cultural,
                    IsFeatured = false,
                    IsActive = true,
                    ImageUrl = "https://images.unsplash.com/photo-1540959733332-eab4deabeeaf?w=800",
                    PackageDetails = "{\"inclusions\":[\"Round-trip flights\",\"Traditional ryokan + modern hotel\",\"JR Pass included\",\"Food tours\",\"Cultural experiences\",\"Tokyo Skytree tickets\"],\"exclusions\":[\"Some meals\",\"Shopping expenses\",\"Optional experiences\"]}",
                    CreatedAt = DateTime.UtcNow
                },
                new TravelPackage
                {
                    Name = "Paris Romantic Getaway",
                    Description = "Fall in love with Paris through romantic walks, fine dining, cultural sites, and charming accommodations.",
                    Destination = "Paris, France",
                    Duration = 4,
                    BasePrice = 2800.00m,
                    Currency = "USD",
                    Category = PackageCategory.Honeymoon,
                    IsFeatured = false,
                    IsActive = true,
                    ImageUrl = "https://images.unsplash.com/photo-1502602898536-47ad22581b52?w=800",
                    PackageDetails = "{\"inclusions\":[\"Round-trip flights\",\"Romantic boutique hotel\",\"Metro passes\",\"Eiffel Tower dinner\",\"Louvre & Orsay tickets\",\"Food & wine tour\"],\"exclusions\":[\"Most lunches\",\"Shopping\",\"Taxi rides\"]}",
                    CreatedAt = DateTime.UtcNow
                }
            };

            context.TravelPackages.AddRange(packages);
            await context.SaveChangesAsync();
            Console.WriteLine($"✅ {packages.Count} travel packages seeded successfully!");
        }
        else
        {
            Console.WriteLine("Travel packages already exist.");
        }
    }
}