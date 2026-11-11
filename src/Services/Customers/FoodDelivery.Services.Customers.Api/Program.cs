using FoodDelivery.BuildingBlocks;
using FoodDelivery.BuildingBlocks.Web;
using FoodDelivery.BuildingBlocks.Messaging;
using FoodDelivery.BuildingBlocks.Persistence;
using FoodDelivery.Services.Customers.Api.Data;
using FoodDelivery.Services.Customers.Api.Features.Customers;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

MongoGuidSetup.Configure();

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddJsonFile("appsettings.Development.local.json", optional: true, reloadOnChange: true);

builder.AddServiceDefaults();
builder.AddNpgsqlDbContext<CustomersDbContext>("customersdb");
builder.AddMongoDBClient("mongodb");
builder.Services.AddBuildingBlocks(typeof(Program).Assembly);
builder.Services.AddScoped<CustomerReadStore>();
builder.Services.AddOpenApi();
builder.Services.AddCors(o => o.AddDefaultPolicy(p => p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));
builder.Host.UseFoodDeliveryMessaging(builder.Configuration, "customers", typeof(Program).Assembly);

var app = builder.Build();
app.UseFoodDeliveryExceptionHandling();
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
    var mongo = scope.ServiceProvider.GetRequiredService<MongoDB.Driver.IMongoClient>();
    var readStore = scope.ServiceProvider.GetRequiredService<CustomerReadStore>();

    await db.Database.EnsureCreatedAsync();
    await MongoReadiness.WaitAsync(mongo);

    var writeCustomers = await db.Customers.AsNoTracking().ToListAsync();
    var readCustomers = await readStore.ListAsync();
    var writeIds = writeCustomers.Select(x => x.Id).ToHashSet();

    foreach (var stale in readCustomers.Where(x => !writeIds.Contains(x.Id)))
    {
        await readStore.DeleteAsync(stale.Id);
    }

    foreach (var customer in writeCustomers)
    {
        await readStore.UpsertAsync(new CustomerReadModel
        {
            Id = customer.Id,
            IdentityId = customer.IdentityId,
            Email = customer.Email,
            FullName = $"{customer.FirstName} {customer.LastName}",
            Phone = customer.Phone
        });
    }
}

app.Run();

public partial class Program;
