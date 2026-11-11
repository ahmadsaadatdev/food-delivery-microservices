using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using FluentValidation;
using FoodDelivery.BuildingBlocks.CQRS;
using FoodDelivery.BuildingBlocks.Messaging;
using FoodDelivery.Services.Identity.Api.Data;
using FoodDelivery.Services.Identity.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
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
    IConfiguration configuration) : ICommandHandler<RegisterUserCommand, RegisterUserResult>
{
    public async Task<RegisterUserResult> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        if (await db.Users.AnyAsync(x => x.Email == email, cancellationToken))
        {
            throw new InvalidOperationException("Email is already registered.");
        }

        var user = User.Register(email, HashPassword(request.Password), request.FirstName, request.LastName);
        db.Users.Add(user);
        await db.SaveChangesAsync(cancellationToken);

        await bus.PublishAsync(new UserRegisteredIntegrationEvent(user.Id, user.Email, user.FirstName, user.LastName));

        var token = CreateJwt(user, configuration);
        return new RegisterUserResult(user.Id, user.Email, token);
    }

    private static string HashPassword(string password)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
        return Convert.ToHexString(bytes);
    }

    private static string CreateJwt(User user, IConfiguration configuration)
    {
        var key = configuration["Jwt:Key"] ?? "FoodDelivery_Dev_Signing_Key_Change_Me_1234567890";
        var issuer = configuration["Jwt:Issuer"] ?? "food-delivery-identity";
        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer,
            issuer,
            [
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim(JwtRegisteredClaimNames.GivenName, user.FirstName),
                new Claim(JwtRegisteredClaimNames.FamilyName, user.LastName)
            ],
            expires: DateTime.UtcNow.AddHours(8),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

public static class RegisterUserEndpoint
{
    public static RouteGroupBuilder MapRegisterUserEndpoint(this RouteGroupBuilder group)
    {
        group.MapPost("/register", async (RegisterUserCommand command, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(command, ct);
            return Results.Created($"/api/v1/identity/users/{result.UserId}", result);
        })
        .WithName("RegisterUser")
        .WithOpenApi();

        return group;
    }
}
