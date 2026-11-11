using FoodDelivery.BuildingBlocks;
using FoodDelivery.BuildingBlocks.Messaging;
using FoodDelivery.BuildingBlocks.Persistence;
using FoodDelivery.Services.Orders.Api.Data;
using FoodDelivery.Services.Orders.Api.Features.Orders;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

MongoGuidSetup.Configure();

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddNpgsqlDbContext<OrdersDbContext>("ordersdb");
builder.AddMongoDBClient("mongodb");
builder.Services.AddBuildingBlocks(typeof(Program).Assembly);
builder.Services.AddScoped<OrderReadStore>();
builder.Services.AddOpenApi();
builder.Services.AddCors(o => o.AddDefaultPolicy(p => p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));
builder.Host.UseFoodDeliveryMessaging(builder.Configuration, "orders", typeof(Program).Assembly);

var app = builder.Build();
app.UseCors();

app.MapOpenApi();
app.MapScalarApiReference(options =>
{
    options.WithTitle("Food Delivery — Orders");
});
app.MapDefaultEndpoints();

var orders = app.MapGroup("/api/v1/orders").WithTags("Orders");
orders.MapOrderEndpoints();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();
    await db.Database.EnsureCreatedAsync();
}

app.Run();

public partial class Program;
