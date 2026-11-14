using FoodDelivery.Services.Customers.Api.Domain;
using FoodDelivery.Services.Identity.Api.Domain;

namespace FoodDelivery.UnitTests.Domain;

[TestClass]
public sealed class CustomerAndUserTests
{
    [TestMethod]
    public void Customer_CreateFromIdentity_SetsFields()
    {
        var identityId = Guid.NewGuid();

        var customer = Customer.CreateFromIdentity(identityId, "a@b.com", "Ahmad", "Saadat");

        Assert.AreEqual(identityId, customer.IdentityId);
        Assert.AreEqual("a@b.com", customer.Email);
        Assert.AreEqual("Ahmad", customer.FirstName);
        Assert.AreEqual("Saadat", customer.LastName);
        Assert.IsNull(customer.Phone);
    }

    [TestMethod]
    public void Customer_UpdateContact_SetsPhone()
    {
        var customer = Customer.CreateFromIdentity(Guid.NewGuid(), "a@b.com", "Ahmad", "Saadat");

        customer.UpdateContact("09120000000");

        Assert.AreEqual("09120000000", customer.Phone);
    }

    [TestMethod]
    public void User_Register_NormalizesEmailAndRaisesEvent()
    {
        var user = User.Register("  Ahmad@Food.Test  ", "HASH", " Ahmad ", " Saadat ");

        Assert.AreEqual("ahmad@food.test", user.Email);
        Assert.AreEqual("HASH", user.PasswordHash);
        Assert.AreEqual("Ahmad", user.FirstName);
        Assert.AreEqual("Saadat", user.LastName);
        Assert.AreEqual(1, user.DomainEvents.Count);
        Assert.IsInstanceOfType(user.DomainEvents.First(), typeof(UserRegisteredDomainEvent));
    }
}
