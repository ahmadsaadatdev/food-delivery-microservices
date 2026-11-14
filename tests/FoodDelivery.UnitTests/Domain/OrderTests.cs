using FoodDelivery.Services.Orders.Api.Domain;

namespace FoodDelivery.UnitTests.Domain;

[TestClass]
public sealed class OrderTests
{
    [TestMethod]
    public void Create_WithLines_CalculatesTotalAndRaisesEvent()
    {
        var customerId = Guid.NewGuid();
        var lines = new[]
        {
            OrderLine.Create(Guid.NewGuid(), "Pizza", 2, 12.5m),
            OrderLine.Create(Guid.NewGuid(), "Burger", 1, 9.9m)
        };

        var order = Order.Create(customerId, lines);

        Assert.AreEqual(customerId, order.CustomerId);
        Assert.AreEqual(OrderStatus.Pending, order.Status);
        Assert.AreEqual(2, order.Lines.Count);
        Assert.AreEqual(34.9m, order.TotalAmount);
        Assert.AreEqual(1, order.DomainEvents.Count);
        Assert.IsInstanceOfType(order.DomainEvents.First(), typeof(OrderCreatedDomainEvent));
    }

    [TestMethod]
    public void Create_WithoutLines_Throws()
    {
        Assert.ThrowsException<InvalidOperationException>(() =>
            Order.Create(Guid.NewGuid(), Array.Empty<OrderLine>()));
    }

    [TestMethod]
    public void OrderLine_Create_WithInvalidQuantity_Throws()
    {
        Assert.ThrowsException<ArgumentOutOfRangeException>(() =>
            OrderLine.Create(Guid.NewGuid(), "Pizza", 0, 10m));
    }

    [TestMethod]
    public void OrderLine_LineTotal_IsQuantityTimesUnitPrice()
    {
        var line = OrderLine.Create(Guid.NewGuid(), "Pizza", 3, 10m);

        Assert.AreEqual(30m, line.LineTotal);
    }
}
