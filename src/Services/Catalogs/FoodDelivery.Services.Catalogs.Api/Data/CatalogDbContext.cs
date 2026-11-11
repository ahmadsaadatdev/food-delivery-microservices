using FoodDelivery.Services.Catalogs.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace FoodDelivery.Services.Catalogs.Api.Data;

public sealed class CatalogDbContext(DbContextOptions<CatalogDbContext> options) : DbContext(options)
{
    public DbSet<Product> Products => Set<Product>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product>(entity =>
        {
            entity.ToTable("products");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(2000);
            entity.Property(x => x.Price).HasPrecision(18, 2);
            entity.Ignore(x => x.DomainEvents);
        });
    }
}
