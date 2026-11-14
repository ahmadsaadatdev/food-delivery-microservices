using System.IdentityModel.Tokens.Jwt;
using FoodDelivery.BuildingBlocks.Auth;
using Microsoft.Extensions.Configuration;

namespace FoodDelivery.UnitTests.Auth;

[TestClass]
public sealed class JwtTokenServiceTests
{
    [TestMethod]
    public void CreateToken_ContainsExpectedClaims()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "FoodDelivery_UnitTest_Signing_Key_1234567890",
                ["Jwt:Issuer"] = "food-delivery-unit-tests"
            })
            .Build();

        var jwt = new JwtTokenService(configuration);
        var userId = Guid.NewGuid();

        var token = jwt.CreateToken(userId, "user@test.com", "Ahmad", "Saadat");

        var handler = new JwtSecurityTokenHandler();
        var parsed = handler.ReadJwtToken(token);

        Assert.AreEqual("food-delivery-unit-tests", parsed.Issuer);
        Assert.AreEqual(userId.ToString(), parsed.Subject);
        Assert.AreEqual("user@test.com", parsed.Claims.First(x => x.Type == JwtRegisteredClaimNames.Email).Value);
        Assert.AreEqual("Ahmad", parsed.Claims.First(x => x.Type == JwtRegisteredClaimNames.GivenName).Value);
        Assert.AreEqual("Saadat", parsed.Claims.First(x => x.Type == JwtRegisteredClaimNames.FamilyName).Value);
    }

    [TestMethod]
    public void CreateToken_WithoutNames_DoesNotIncludeNameClaims()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "FoodDelivery_UnitTest_Signing_Key_1234567890",
                ["Jwt:Issuer"] = "food-delivery-unit-tests"
            })
            .Build();

        var jwt = new JwtTokenService(configuration);
        var token = jwt.CreateToken(Guid.NewGuid(), "user@test.com");
        var parsed = new JwtSecurityTokenHandler().ReadJwtToken(token);

        Assert.IsFalse(parsed.Claims.Any(x => x.Type == JwtRegisteredClaimNames.GivenName));
        Assert.IsFalse(parsed.Claims.Any(x => x.Type == JwtRegisteredClaimNames.FamilyName));
    }
}
