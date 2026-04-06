using EmlakPortali.Api.Data;
using EmlakPortali.Api.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace EmlakPortali.Api.Startup;

public static class SeedData
{
    public static async Task EnsureSeedDataAsync(WebApplication app)
    {
        using var scope = app.Services.CreateScope();

        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();

        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var roles = new[] { "Admin", "User" };
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new ApplicationRole { Name = role });
            }
        }

        // Default admin: admin@emlak.local / Admin123*
        var adminEmail = "admin@emlak.local";
        var admin = await userManager.Users.FirstOrDefaultAsync(x => x.Email == adminEmail);
        if (admin is null)
        {
            admin = new ApplicationUser
            {
                Email = adminEmail,
                UserName = adminEmail,
                EmailConfirmed = true,
                FullName = "System Admin",
                IsActive = true,
                Created = DateTime.UtcNow
            };
            var created = await userManager.CreateAsync(admin, "Admin123*");
            if (created.Succeeded)
            {
                await userManager.AddToRoleAsync(admin, "Admin");
            }
        }

        if (!await db.Cities.AnyAsync())
        {
            var istanbul = new City { Name = "İstanbul", IsActive = true, Created = DateTime.UtcNow };
            var ankara = new City { Name = "Ankara", IsActive = true, Created = DateTime.UtcNow };

            db.Cities.AddRange(istanbul, ankara);
            await db.SaveChangesAsync();

            db.Districts.AddRange(
                new District { CityId = istanbul.Id, Name = "Kadıköy", IsActive = true, Created = DateTime.UtcNow },
                new District { CityId = istanbul.Id, Name = "Beşiktaş", IsActive = true, Created = DateTime.UtcNow },
                new District { CityId = ankara.Id, Name = "Çankaya", IsActive = true, Created = DateTime.UtcNow },
                new District { CityId = ankara.Id, Name = "Keçiören", IsActive = true, Created = DateTime.UtcNow }
            );
            await db.SaveChangesAsync();
        }

        if (!await db.Categories.AnyAsync())
        {
            db.Categories.AddRange(
                new Category { Name = "Daire", IsActive = true, Created = DateTime.UtcNow },
                new Category { Name = "Arsa", IsActive = true, Created = DateTime.UtcNow },
                new Category { Name = "İş Yeri", IsActive = true, Created = DateTime.UtcNow }
            );
            await db.SaveChangesAsync();
        }
    }
}

