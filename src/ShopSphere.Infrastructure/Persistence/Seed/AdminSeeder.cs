using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using ShopSphere.Domain.Common;
using ShopSphere.Domain.Entities;
using ShopSphere.Infrastructure.Persistence.Context;

namespace ShopSphere.Infrastructure.Persistence.Seed;

public static class AdminSeeder
{
    public static async Task SeedAsync(
        ShopSphereDbContext context,
        IPasswordHasher<User> passwordHasher,
        IConfiguration configuration)
    {
        var adminEmail = configuration["AdminUser:Email"];
        var adminPassword = configuration["AdminUser:Password"];

        if (string.IsNullOrWhiteSpace(adminEmail) ||
            string.IsNullOrWhiteSpace(adminPassword))
        {
            return;
        }

        adminEmail = adminEmail.Trim().ToLowerInvariant();

        var adminExists = await context.Users
            .IgnoreQueryFilters()
            .AnyAsync(x => x.Email == adminEmail);

        if (adminExists)
        {
            return;
        }

        var admin = new User
        {
            FirstName = "Admin",
            LastName = "User",
            Email = adminEmail,
            Role = UserRoles.Admin,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        admin.PasswordHash = passwordHasher.HashPassword(
            admin,
            adminPassword);

        context.Users.Add(admin);

        await context.SaveChangesAsync();
    }
}