using EmlakPortali.Api.Models.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace EmlakPortali.Api.Data;

public class AppDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<City> Cities => Set<City>();
    public DbSet<District> Districts => Set<District>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Listing> Listings => Set<Listing>();
    public DbSet<ListingImage> ListingImages => Set<ListingImage>();
    public DbSet<FavoriteListing> FavoriteListings => Set<FavoriteListing>();
    public DbSet<ContactMessage> ContactMessages => Set<ContactMessage>();
    public DbSet<Project> Projects => Set<Project>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<City>(b =>
        {
            b.Property(x => x.Name).HasMaxLength(100).IsRequired();
            b.HasIndex(x => x.Name).IsUnique();
        });

        builder.Entity<Category>(b =>
        {
            b.Property(x => x.Name).HasMaxLength(100).IsRequired();
            b.HasIndex(x => x.Name).IsUnique();
        });

        builder.Entity<District>(b =>
        {
            b.Property(x => x.Name).HasMaxLength(120).IsRequired();
            b.HasIndex(x => new { x.CityId, x.Name }).IsUnique();
        });

        builder.Entity<Listing>(b =>
        {
            b.Property(x => x.Title).HasMaxLength(200).IsRequired();
            b.Property(x => x.Description).HasMaxLength(4000).IsRequired();
            b.Property(x => x.Price).HasPrecision(18, 2);

            b.HasOne(x => x.City)
                .WithMany()
                .HasForeignKey(x => x.CityId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(x => x.Category)
                .WithMany(c => c.Listings)
                .HasForeignKey(x => x.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(x => x.District)
                .WithMany()
                .HasForeignKey(x => x.DistrictId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(x => x.OwnerUser)
                .WithMany()
                .HasForeignKey(x => x.OwnerUserId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasIndex(x => x.IsApproved);
            b.HasIndex(x => x.IsActive);
            b.HasIndex(x => x.Created);
        });

        builder.Entity<ListingImage>(b =>
        {
            b.Property(x => x.Url).HasMaxLength(500).IsRequired();
            b.HasIndex(x => new { x.ListingId, x.SortOrder }).IsUnique();
        });

        builder.Entity<FavoriteListing>(b =>
        {
            b.HasIndex(x => new { x.UserId, x.ListingId }).IsUnique();
        });



        builder.Entity<Project>(b =>
        {
            b.Property(x => x.Name).HasMaxLength(200).IsRequired();
            b.Property(x => x.Slug).HasMaxLength(200).IsRequired();
            b.Property(x => x.City).HasMaxLength(100);
            b.Property(x => x.District).HasMaxLength(100);
            b.Property(x => x.Description).HasMaxLength(4000);
            b.Property(x => x.RoomTypes).HasMaxLength(200);
            b.Property(x => x.Status).HasMaxLength(50);
            b.Property(x => x.DeliveryDate).HasMaxLength(50);
            b.HasIndex(x => x.Slug).IsUnique();
            b.HasIndex(x => x.Created);
        });
    }
}
