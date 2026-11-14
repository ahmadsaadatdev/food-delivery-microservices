using FoodDelivery.Services.Identity.Api.Features.Login;
using FoodDelivery.Services.Identity.Api.Features.Register;

namespace FoodDelivery.UnitTests.Validators;

[TestClass]
public sealed class RegisterAndLoginValidatorTests
{
    [TestMethod]
    public async Task Register_ValidCommand_IsValid()
    {
        var validator = new RegisterUserValidator();

        var result = await validator.ValidateAsync(
            new RegisterUserCommand("user@test.com", "Pass@123", "Ahmad", "Saadat"));

        Assert.IsTrue(result.IsValid);
    }

    [TestMethod]
    public async Task Register_ShortPassword_IsInvalid()
    {
        var validator = new RegisterUserValidator();

        var result = await validator.ValidateAsync(
            new RegisterUserCommand("user@test.com", "123", "Ahmad", "Saadat"));

        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(x => x.PropertyName == nameof(RegisterUserCommand.Password)));
    }

    [TestMethod]
    public async Task Register_InvalidEmail_IsInvalid()
    {
        var validator = new RegisterUserValidator();

        var result = await validator.ValidateAsync(
            new RegisterUserCommand("not-an-email", "Pass@123", "Ahmad", "Saadat"));

        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(x => x.PropertyName == nameof(RegisterUserCommand.Email)));
    }

    [TestMethod]
    public async Task Login_EmptyPassword_IsInvalid()
    {
        var validator = new LoginUserValidator();

        var result = await validator.ValidateAsync(new LoginUserCommand("user@test.com", ""));

        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(x => x.PropertyName == nameof(LoginUserCommand.Password)));
    }

    [TestMethod]
    public async Task Login_ValidCommand_IsValid()
    {
        var validator = new LoginUserValidator();

        var result = await validator.ValidateAsync(new LoginUserCommand("user@test.com", "secret"));

        Assert.IsTrue(result.IsValid);
    }
}
