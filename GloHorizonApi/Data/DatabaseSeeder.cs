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
}