using System.Security.Cryptography;
using System.Text;
using FluentValidation;
using FoodDelivery.BuildingBlocks.Auth;
using FoodDelivery.BuildingBlocks.CQRS;
using FoodDelivery.Services.Identity.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace FoodDelivery.Services.Identity.Api.Features.Login;

public sealed record LoginUserCommand(string Email, string Password) : ICommand<LoginUserResult>;

public sealed record LoginUserResult(Guid UserId, string Email, string AccessToken);

public sealed class LoginUserValidator : AbstractValidator<LoginUserCommand>
{
    public LoginUserValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty();
    }
}

public sealed class LoginUserHandler(IdentityDbContext db, JwtTokenService jwt)
    : ICommandHandler<LoginUserCommand, LoginUserResult>
{
    public async Task<LoginUserResult> Handle(LoginUserCommand request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await db.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Email == email, cancellationToken)
            ?? throw new UnauthorizedAccessException("Invalid credentials.");

        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(request.Password)));
        if (!string.Equals(user.PasswordHash, hash, StringComparison.OrdinalIgnoreCase))
        {
            throw new UnauthorizedAccessException("Invalid credentials.");
        }

        return new LoginUserResult(user.Id, user.Email, jwt.CreateToken(user.Id, user.Email, user.FirstName, user.LastName));
    }
}

public static class LoginUserEndpoint
{
    public static RouteGroupBuilder MapLoginUserEndpoint(this RouteGroupBuilder group)
    {
        group.MapPost("/login", async (LoginUserCommand command, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(command, ct);
            return Results.Ok(result);
        })
        .WithName("LoginUser");

        return group;
    }
}
