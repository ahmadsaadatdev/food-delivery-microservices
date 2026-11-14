using FoodDelivery.Services.Orders.Api.Features.Orders;

namespace FoodDelivery.UnitTests.Validators;

[TestClass]
public sealed class CreateOrderValidatorTests
{
    private readonly CreateOrderValidator _validator = new();

    [TestMethod]
    public async Task Validate_ValidCommand_IsValid()
    {
        var command = new CreateOrderCommand(
            Guid.NewGuid(),
            [
                new CreateOrderLineDto(Guid.NewGuid(), "Pizza", 2, 12.5m)
            ]);

        var result = await _validator.ValidateAsync(command);

        Assert.IsTrue(result.IsValid);
    }

    [TestMethod]
    public async Task Validate_EmptyCustomerId_IsInvalid()
    {
        var command = new CreateOrderCommand(
            Guid.Empty,
            [
                new CreateOrderLineDto(Guid.NewGuid(), "Pizza", 1, 10m)
            ]);

        var result = await _validator.ValidateAsync(command);

        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(x => x.PropertyName == nameof(CreateOrderCommand.CustomerId)));
    }

    [TestMethod]
    public async Task Validate_EmptyLines_IsInvalid()
    {
        var command = new CreateOrderCommand(Guid.NewGuid(), []);

        var result = await _validator.ValidateAsync(command);

        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(x => x.PropertyName == nameof(CreateOrderCommand.Lines)));
    }

    [TestMethod]
    public async Task Validate_LineWithoutProductName_IsInvalid()
    {
        var command = new CreateOrderCommand(
            Guid.NewGuid(),
            [
                new CreateOrderLineDto(Guid.NewGuid(), "", 1, 10m)
            ]);

        var result = await _validator.ValidateAsync(command);

        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(x => x.PropertyName.Contains("ProductName")));
    }
}
