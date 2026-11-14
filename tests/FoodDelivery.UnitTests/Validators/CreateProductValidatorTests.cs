using FoodDelivery.Services.Catalogs.Api.Features.Products;

namespace FoodDelivery.UnitTests.Validators;

[TestClass]
public sealed class CreateProductValidatorTests
{
    private readonly CreateProductValidator _validator = new();

    [TestMethod]
    public async Task Validate_ValidCommand_IsValid()
    {
        var result = await _validator.ValidateAsync(
            new CreateProductCommand("Pizza", "Nice", 12.5m, 10));

        Assert.IsTrue(result.IsValid);
    }

    [TestMethod]
    public async Task Validate_EmptyName_IsInvalid()
    {
        var result = await _validator.ValidateAsync(
            new CreateProductCommand("", "Nice", 12.5m, 10));

        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(x => x.PropertyName == nameof(CreateProductCommand.Name)));
    }

    [TestMethod]
    public async Task Validate_NonPositivePrice_IsInvalid()
    {
        var result = await _validator.ValidateAsync(
            new CreateProductCommand("Pizza", "Nice", 0m, 10));

        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(x => x.PropertyName == nameof(CreateProductCommand.Price)));
    }

    [TestMethod]
    public async Task Validate_NegativeStock_IsInvalid()
    {
        var result = await _validator.ValidateAsync(
            new CreateProductCommand("Pizza", "Nice", 10m, -1));

        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(x => x.PropertyName == nameof(CreateProductCommand.Stock)));
    }
}
