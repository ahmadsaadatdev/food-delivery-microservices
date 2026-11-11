using FoodDelivery.BuildingBlocks;
using FoodDelivery.BuildingBlocks.Messaging;
using FoodDelivery.BuildingBlocks.Persistence;
using FoodDelivery.Services.Customers.Api.Data;
using FoodDelivery.Services.Customers.Api.Features.Customers;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

MongoGuidSetup.Configure();

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddNpgsqlDbContext<CustomersDbContext>("customersdb");
builder.AddMongoDBClient("mongodb");
builder.Services.AddBuildingBlocks(typeof(Program).Assembly);
builder.Services.AddScoped<CustomerReadStore>();
builder.Services.AddOpenApi();
builder.Services.AddCors(o => o.AddDefaultPolicy(p => p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));
builder.Host.UseFoodDeliveryMessaging(builder.Configuration, "customers", typeof(Program).Assembly);

var app = builder.Build();
app.UseCors();

app.MapOpenApi();
app.MapScalarApiReference(options =>
{
    options.WithTitle("Food Delivery — Customers");
});
app.MapDefaultEndpoints();

var customers = app.MapGroup("/api/v1/customers").WithTags("Customers");
customers.MapCustomerEndpoints();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<CustomersDbContext>();
    await db.Database.EnsureCreatedAsync();
}

app.Run();

public partial class Program;
