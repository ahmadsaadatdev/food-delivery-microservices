using FoodDelivery.Services.Orders.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace FoodDelivery.Services.Orders.Api.Data;

public sealed class OrdersDbContext(DbContextOptions<OrdersDbContext> options) : DbContext(options)
{
    public DbSet<Order> Orders => Set<Order>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Order>(entity =>
        {
            entity.ToTable("orders");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.TotalAmount).HasPrecision(18, 2);
            entity.Ignore(x => x.DomainEvents);
            entity.Navigation(x => x.Lines).HasField("_lines");
            entity.OwnsMany(x => x.Lines, lines =>
            {
                lines.ToTable("order_lines");
                lines.WithOwner().HasForeignKey("OrderId");
                lines.Property<Guid>("Id");
                lines.HasKey("Id");
                lines.Property(x => x.ProductName).HasMaxLength(200);
                lines.Property(x => x.UnitPrice).HasPrecision(18, 2);
            });
        });
    }
}
