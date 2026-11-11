using System.Text;
using FoodDelivery.BuildingBlocks;
using FoodDelivery.BuildingBlocks.Web;
using FoodDelivery.BuildingBlocks.Messaging;
using FoodDelivery.Services.Identity.Api.Data;
using FoodDelivery.Services.Identity.Api.Features.Login;
using FoodDelivery.Services.Identity.Api.Features.Register;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddJsonFile("appsettings.Development.local.json", optional: true, reloadOnChange: true);

builder.AddServiceDefaults();
builder.AddNpgsqlDbContext<IdentityDbContext>("identitydb");
builder.Services.AddBuildingBlocks(typeof(Program).Assembly);
builder.Services.AddOpenApi();
builder.Services.AddCors(o => o.AddDefaultPolicy(p => p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

var jwtKey = builder.Configuration["Jwt:Key"] ?? "FoodDelivery_Dev_Signing_Key_Change_Me_1234567890";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "food-delivery-identity";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtIssuer,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });
builder.Services.AddAuthorization();
builder.Host.UseFoodDeliveryMessaging(builder.Configuration, "identity", typeof(Program).Assembly);

var app = builder.Build();
app.UseFoodDeliveryExceptionHandling();
app.UseCors();

app.MapOpenApi();
app.MapScalarApiReference(options =>
{
    options.WithTitle("Food Delivery — Identity");
});
app.UseAuthentication();
app.UseAuthorization();
app.MapDefaultEndpoints();

var identity = app.MapGroup("/api/v1/identity").WithTags("Identity");
identity.MapRegisterUserEndpoint();
identity.MapLoginUserEndpoint();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
    await db.Database.EnsureCreatedAsync();
}

app.Run();

public partial class Program;
