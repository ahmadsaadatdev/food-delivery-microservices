using FoodDelivery.BuildingBlocks.Domain;

namespace FoodDelivery.Services.Orders.Api.Domain;

public enum OrderStatus
{
    Pending = 0,
    Confirmed = 1,
    Cancelled = 2
}

public sealed class Order : Entity<Guid>
{
    private readonly List<OrderLine> _lines = [];

    public Guid CustomerId { get; private set; }
    public OrderStatus Status { get; private set; }
    public decimal TotalAmount { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public IReadOnlyCollection<OrderLine> Lines => _lines.AsReadOnly();

    private Order()
    {
    }

    public static Order Create(Guid customerId, IEnumerable<OrderLine> lines)
    {
        var order = new Order
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId,
            Status = OrderStatus.Pending,
            CreatedAtUtc = DateTime.UtcNow
        };

        foreach (var line in lines)
        {
            order._lines.Add(line);
        }

        if (order._lines.Count == 0)
        {
            throw new InvalidOperationException("Order must contain at least one line.");
        }

        order.TotalAmount = order._lines.Sum(x => x.LineTotal);
        order.Raise(new OrderCreatedDomainEvent(order.Id, order.CustomerId, order.TotalAmount));
        return order;
    }
}

public sealed class OrderLine
{
    public Guid ProductId { get; private set; }
    public string ProductName { get; private set; } = default!;
    public int Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }
    public decimal LineTotal => Quantity * UnitPrice;

    private OrderLine()
    {
    }

    public static OrderLine Create(Guid productId, string productName, int quantity, decimal unitPrice)
    {
        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity));
        }

        return new OrderLine
        {
            ProductId = productId,
            ProductName = productName,
            Quantity = quantity,
            UnitPrice = unitPrice
        };
    }
}

public sealed record OrderCreatedDomainEvent(Guid OrderId, Guid CustomerId, decimal TotalAmount) : DomainEvent;
