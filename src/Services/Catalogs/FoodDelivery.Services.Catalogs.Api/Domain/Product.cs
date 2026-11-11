using FoodDelivery.BuildingBlocks.Domain;

namespace FoodDelivery.Services.Catalogs.Api.Domain;

public sealed class Product : Entity<Guid>
{
    public string Name { get; private set; } = default!;
    public string Description { get; private set; } = default!;
    public decimal Price { get; private set; }
    public int Stock { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    private Product()
    {
    }

    public static Product Create(string name, string description, decimal price, int stock)
    {
        if (price <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(price));
        }

        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            Description = description.Trim(),
            Price = price,
            Stock = stock,
            CreatedAtUtc = DateTime.UtcNow
        };

        product.Raise(new ProductCreatedDomainEvent(product.Id, product.Name, product.Price, product.Stock));
        return product;
    }

    public void UpdateStock(int stock)
    {
        if (stock < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(stock));
        }

        Stock = stock;
    }
}

public sealed record ProductCreatedDomainEvent(Guid ProductId, string Name, decimal Price, int Stock) : DomainEvent;
