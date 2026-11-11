using FoodDelivery.BuildingBlocks;
using FoodDelivery.BuildingBlocks.Web;
using FoodDelivery.BuildingBlocks.Messaging;
using FoodDelivery.BuildingBlocks.Persistence;
using FoodDelivery.Services.Orders.Api.Data;
using FoodDelivery.Services.Orders.Api.Features.Orders;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

MongoGuidSetup.Configure();

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddJsonFile("appsettings.Development.local.json", optional: true, reloadOnChange: true);

builder.AddServiceDefaults();
builder.AddNpgsqlDbContext<OrdersDbContext>("ordersdb");
builder.AddMongoDBClient("mongodb");
builder.Services.AddBuildingBlocks(typeof(Program).Assembly);
builder.Services.AddScoped<OrderReadStore>();
builder.Services.AddOpenApi();
builder.Services.AddCors(o => o.AddDefaultPolicy(p => p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));
builder.Host.UseFoodDeliveryMessaging(builder.Configuration, "orders", typeof(Program).Assembly);

var app = builder.Build();
app.UseFoodDeliveryExceptionHandling();
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
    var mongo = scope.ServiceProvider.GetRequiredService<MongoDB.Driver.IMongoClient>();
    var readStore = scope.ServiceProvider.GetRequiredService<OrderReadStore>();

    await db.Database.EnsureCreatedAsync();
    await MongoReadiness.WaitAsync(mongo);

    var writeOrders = await db.Orders.AsNoTracking().Include(x => x.Lines).ToListAsync();
    var readOrders = await readStore.ListAsync();
    var writeIds = writeOrders.Select(x => x.Id).ToHashSet();

    foreach (var stale in readOrders.Where(x => !writeIds.Contains(x.Id)))
    {
        await readStore.DeleteAsync(stale.Id);
    }

    foreach (var order in writeOrders)
    {
        await readStore.UpsertAsync(new OrderReadModel
        {
            Id = order.Id,
            CustomerId = order.CustomerId,
            Status = order.Status.ToString(),
            TotalAmount = order.TotalAmount,
            CreatedAtUtc = order.CreatedAtUtc,
            Lines = order.Lines.Select(x => new OrderLineReadModel
            {
                ProductId = x.ProductId,
                ProductName = x.ProductName,
                Quantity = x.Quantity,
                UnitPrice = x.UnitPrice
            }).ToList()
        });
    }
}

app.Run();

public partial class Program;
