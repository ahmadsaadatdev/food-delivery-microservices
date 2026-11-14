using System.Security.Cryptography;
using System.Text;
using FluentValidation;
using FoodDelivery.BuildingBlocks.Auth;
using FoodDelivery.BuildingBlocks.CQRS;
using FoodDelivery.BuildingBlocks.Messaging;
using FoodDelivery.Services.Identity.Api.Data;
using FoodDelivery.Services.Identity.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Wolverine;

namespace FoodDelivery.Services.Identity.Api.Features.Register;

public sealed record RegisterUserCommand(
    string Email,
    string Password,
    string FirstName,
    string LastName) : ICommand<RegisterUserResult>;

public sealed record RegisterUserResult(Guid UserId, string Email, string AccessToken);

public sealed class RegisterUserValidator : AbstractValidator<RegisterUserCommand>
{
    public RegisterUserValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty().MinimumLength(6);
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
    }
}

public sealed class RegisterUserHandler(
    IdentityDbContext db,
    IMessageBus bus,
    JwtTokenService jwt) : ICommandHandler<RegisterUserCommand, RegisterUserResult>
{
    public async Task<RegisterUserResult> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        if (await db.Users.AsNoTracking().AnyAsync(x => x.Email == email, cancellationToken))
        {
            throw new InvalidOperationException("Email is already registered.");
        }

        var user = User.Register(email, HashPassword(request.Password), request.FirstName, request.LastName);
        db.Users.Add(user);
        await db.SaveChangesAsync(cancellationToken);

        await bus.PublishAsync(new UserRegisteredIntegrationEvent(user.Id, user.Email, user.FirstName, user.LastName));

        return new RegisterUserResult(
            user.Id,
            user.Email,
            jwt.CreateToken(user.Id, user.Email, user.FirstName, user.LastName));
    }

    private static string HashPassword(string password)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
        return Convert.ToHexString(bytes);
    }
}

public static class RegisterUserEndpoint
{
    public static RouteGroupBuilder MapRegisterUserEndpoint(this RouteGroupBuilder group)
    {
        group.MapPost("/register", async (RegisterUserCommand command, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(command, ct);
            return Results.Ok(result);
        })
        .WithName("RegisterUser");

        return group;
    }
}
