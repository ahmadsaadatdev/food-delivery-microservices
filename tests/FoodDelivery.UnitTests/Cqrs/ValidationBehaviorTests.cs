using FluentValidation;
using FoodDelivery.BuildingBlocks.CQRS;
using MediatR;

namespace FoodDelivery.UnitTests.Cqrs;

[TestClass]
public sealed class ValidationBehaviorTests
{
    private sealed record SampleRequest(string Name) : IRequest<string>;

    private sealed class SampleValidator : AbstractValidator<SampleRequest>
    {
        public SampleValidator()
        {
            RuleFor(x => x.Name).NotEmpty();
        }
    }

    [TestMethod]
    public async Task Handle_WhenValid_CallsNext()
    {
        var behavior = new ValidationBehavior<SampleRequest, string>([new SampleValidator()]);
        var called = false;

        var result = await behavior.Handle(
            new SampleRequest("ok"),
            _ =>
            {
                called = true;
                return Task.FromResult("done");
            },
            CancellationToken.None);

        Assert.IsTrue(called);
        Assert.AreEqual("done", result);
    }

    [TestMethod]
    public async Task Handle_WhenInvalid_ThrowsValidationException()
    {
        var behavior = new ValidationBehavior<SampleRequest, string>([new SampleValidator()]);

        await Assert.ThrowsExceptionAsync<ValidationException>(() =>
            behavior.Handle(
                new SampleRequest(""),
                _ => Task.FromResult("done"),
                CancellationToken.None));
    }

    [TestMethod]
    public async Task Handle_WhenNoValidators_CallsNext()
    {
        var behavior = new ValidationBehavior<SampleRequest, string>([]);

        var result = await behavior.Handle(
            new SampleRequest(""),
            _ => Task.FromResult("skipped"),
            CancellationToken.None);

        Assert.AreEqual("skipped", result);
    }
}
