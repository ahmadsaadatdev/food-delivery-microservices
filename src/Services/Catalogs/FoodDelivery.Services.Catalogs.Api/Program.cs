using FoodDelivery.BuildingBlocks;
using FoodDelivery.BuildingBlocks.Messaging;
using FoodDelivery.BuildingBlocks.Persistence;
using FoodDelivery.BuildingBlocks.Web;
using FoodDelivery.Services.Catalogs.Api.Data;
using FoodDelivery.Services.Catalogs.Api.Domain;
using FoodDelivery.Services.Catalogs.Api.Features.Products;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

MongoGuidSetup.Configure();

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddJsonFile("appsettings.Development.local.json", optional: true, reloadOnChange: true);

builder.AddServiceDefaults();
builder.AddNpgsqlDbContext<CatalogDbContext>("catalogsdb");
builder.AddMongoDBClient("mongodb");
builder.Services.AddBuildingBlocks(typeof(Program).Assembly);
builder.Services.AddScoped<ProductReadStore>();
builder.Services.AddOpenApi();
builder.Services.AddCors(o => o.AddDefaultPolicy(p => p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));
builder.Host.UseFoodDeliveryMessaging(builder.Configuration, "catalogs", typeof(Program).Assembly);

var app = builder.Build();
app.UseFoodDeliveryExceptionHandling();
app.UseCors();

app.MapOpenApi();
app.MapScalarApiReference(options => options.WithTitle("Food Delivery — Catalogs"));
app.MapDefaultEndpoints();

app.MapGet("/demo", async (IWebHostEnvironment env) =>
{
    var www = Path.Combine(env.ContentRootPath, "wwwroot", "demo.html");
    var repo = Path.GetFullPath(Path.Combine(env.ContentRootPath, "..", "..", "..", "..", "deployments", "demo", "index.html"));
    var path = File.Exists(www) ? www : repo;
    return File.Exists(path)
        ? Results.Content(await File.ReadAllTextAsync(path), "text/html; charset=utf-8")
        : Results.NotFound("Demo page missing");
});

var products = app.MapGroup("/api/v1/products").WithTags("Products");
products.MapProductEndpoints();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
    var mongo = scope.ServiceProvider.GetRequiredService<MongoDB.Driver.IMongoClient>();
    var readStore = scope.ServiceProvider.GetRequiredService<ProductReadStore>();
    await db.Database.EnsureCreatedAsync();
    await MongoReadiness.WaitAsync(mongo);

    if (!await db.Products.AnyAsync())
    {
        var pizza = Product.Create("Margherita Pizza", "Classic tomato and mozzarella", 12.50m, 50);
        var burger = Product.Create("Cheese Burger", "Beef patty with cheddar", 9.90m, 80);
        db.Products.AddRange(pizza, burger);
        await db.SaveChangesAsync();
    }

    // Keep Mongo read model in sync with Postgres write model (avoids duplicate/stale seeds).
    var writeProducts = await db.Products.AsNoTracking().ToListAsync();
    var readProducts = await readStore.ListAsync();
    var writeIds = writeProducts.Select(x => x.Id).ToHashSet();

    foreach (var stale in readProducts.Where(x => !writeIds.Contains(x.Id)))
    {
        await readStore.DeleteAsync(stale.Id);
    }

    foreach (var product in writeProducts)
    {
        await readStore.UpsertAsync(new ProductReadModel
        {
            Id = product.Id,
            Name = product.Name,
            Description = product.Description,
            Price = product.Price,
            Stock = product.Stock
        });
    }
}

app.Run();

public partial class Program;
