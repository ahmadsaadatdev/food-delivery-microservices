using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace FoodDelivery.BuildingBlocks.Auth;

public sealed class JwtTokenService
{
    private readonly SigningCredentials _credentials;
    private readonly string _issuer;
    private readonly JwtSecurityTokenHandler _handler = new();

    public JwtTokenService(IConfiguration configuration)
    {
        var key = configuration["Jwt:Key"] ?? "FoodDelivery_Dev_Signing_Key_Change_Me_1234567890";
        _issuer = configuration["Jwt:Issuer"] ?? "food-delivery-identity";
        _credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
            SecurityAlgorithms.HmacSha256);
    }

    public string CreateToken(Guid userId, string email, string? firstName = null, string? lastName = null, TimeSpan? lifetime = null)
    {
        var claims = new List<Claim>(4)
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(JwtRegisteredClaimNames.Email, email)
        };

        if (!string.IsNullOrWhiteSpace(firstName))
        {
            claims.Add(new Claim(JwtRegisteredClaimNames.GivenName, firstName));
        }

        if (!string.IsNullOrWhiteSpace(lastName))
        {
            claims.Add(new Claim(JwtRegisteredClaimNames.FamilyName, lastName));
        }

        var token = new JwtSecurityToken(
            _issuer,
            _issuer,
            claims,
            expires: DateTime.UtcNow.Add(lifetime ?? TimeSpan.FromHours(8)),
            signingCredentials: _credentials);

        return _handler.WriteToken(token);
    }
}
