using FoodDelivery.Services.Catalogs.Api.Domain;

namespace FoodDelivery.UnitTests.Domain;

[TestClass]
public sealed class ProductTests
{
    [TestMethod]
    public void Create_WithValidData_SetsFieldsAndRaisesDomainEvent()
    {
        var product = Product.Create("  Pizza  ", "  tasty  ", 12.5m, 10);

        Assert.AreNotEqual(Guid.Empty, product.Id);
        Assert.AreEqual("Pizza", product.Name);
        Assert.AreEqual("tasty", product.Description);
        Assert.AreEqual(12.5m, product.Price);
        Assert.AreEqual(10, product.Stock);
        Assert.AreEqual(1, product.DomainEvents.Count);
        Assert.IsInstanceOfType(product.DomainEvents.First(), typeof(ProductCreatedDomainEvent));
    }

    [TestMethod]
    public void Create_WithNonPositivePrice_Throws()
    {
        Assert.ThrowsException<ArgumentOutOfRangeException>(() =>
            Product.Create("Burger", "desc", 0m, 5));
    }

    [TestMethod]
    public void UpdateStock_WithValidValue_UpdatesStock()
    {
        var product = Product.Create("Soup", "hot", 5m, 20);

        product.UpdateStock(7);

        Assert.AreEqual(7, product.Stock);
    }

    [TestMethod]
    public void UpdateStock_WithNegative_Throws()
    {
        var product = Product.Create("Soup", "hot", 5m, 20);

        Assert.ThrowsException<ArgumentOutOfRangeException>(() => product.UpdateStock(-1));
    }

    [TestMethod]
    public void DecreaseStock_WithEnoughStock_Decreases()
    {
        var product = Product.Create("Soup", "hot", 5m, 20);

        product.DecreaseStock(4);

        Assert.AreEqual(16, product.Stock);
    }

    [TestMethod]
    public void DecreaseStock_WithInsufficientStock_Throws()
    {
        var product = Product.Create("Soup", "hot", 5m, 2);

        Assert.ThrowsException<InvalidOperationException>(() => product.DecreaseStock(3));
    }

    [TestMethod]
    public void ClearDomainEvents_RemovesRaisedEvents()
    {
        var product = Product.Create("Soup", "hot", 5m, 2);

        product.ClearDomainEvents();

        Assert.AreEqual(0, product.DomainEvents.Count);
    }
}
