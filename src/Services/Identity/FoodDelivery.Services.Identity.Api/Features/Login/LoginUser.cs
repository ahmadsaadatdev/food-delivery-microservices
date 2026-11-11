using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using FluentValidation;
using FoodDelivery.BuildingBlocks.CQRS;
using FoodDelivery.Services.Identity.Api.Data;
using FoodDelivery.Services.Identity.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

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

public sealed class LoginUserHandler(IdentityDbContext db, IConfiguration configuration)
    : ICommandHandler<LoginUserCommand, LoginUserResult>
{
    public async Task<LoginUserResult> Handle(LoginUserCommand request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await db.Users.SingleOrDefaultAsync(x => x.Email == email, cancellationToken)
            ?? throw new UnauthorizedAccessException("Invalid credentials.");

        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(request.Password)));
        if (!string.Equals(user.PasswordHash, hash, StringComparison.OrdinalIgnoreCase))
        {
            throw new UnauthorizedAccessException("Invalid credentials.");
        }

        return new LoginUserResult(user.Id, user.Email, CreateJwt(user, configuration));
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
                new Claim(JwtRegisteredClaimNames.Email, user.Email)
            ],
            expires: DateTime.UtcNow.AddHours(8),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

public static class LoginUserEndpoint
{
    public static RouteGroupBuilder MapLoginUserEndpoint(this RouteGroupBuilder group)
    {
        group.MapPost("/login", async (LoginUserCommand command, IMediator mediator, CancellationToken ct) =>
        {
            try
            {
                var result = await mediator.Send(command, ct);
                return Results.Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Results.Unauthorized();
            }
        })
        .WithName("LoginUser")
        .WithOpenApi();

        return group;
    }
}
