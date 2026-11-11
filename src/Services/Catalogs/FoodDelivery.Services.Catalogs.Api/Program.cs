using FoodDelivery.BuildingBlocks;
using FoodDelivery.BuildingBlocks.Messaging;
using FoodDelivery.BuildingBlocks.Persistence;
using FoodDelivery.Services.Catalogs.Api.Data;
using FoodDelivery.Services.Catalogs.Api.Domain;
using FoodDelivery.Services.Catalogs.Api.Features.Products;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

MongoGuidSetup.Configure();

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddNpgsqlDbContext<CatalogDbContext>("catalogsdb");
builder.AddMongoDBClient("mongodb");
builder.Services.AddBuildingBlocks(typeof(Program).Assembly);
builder.Services.AddScoped<ProductReadStore>();
builder.Services.AddOpenApi();
builder.Services.AddCors(o => o.AddDefaultPolicy(p => p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));
builder.Host.UseFoodDeliveryMessaging(builder.Configuration, "catalogs", typeof(Program).Assembly);

var app = builder.Build();
app.UseCors();

app.MapOpenApi();
app.MapScalarApiReference(options =>
{
    options.WithTitle("Food Delivery — Catalogs");
});
app.MapDefaultEndpoints();

app.MapGet("/demo", () =>
{
    var candidates = new[]
    {
        Path.GetFullPath(Path.Combine(app.Environment.ContentRootPath, "..", "..", "..", "..", "deployments", "demo", "index.html")),
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "deployments", "demo", "index.html")),
        Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "deployments", "demo", "index.html"))
    };

    var htmlPath = candidates.FirstOrDefault(File.Exists);
    return htmlPath is not null
        ? Results.Content(File.ReadAllText(htmlPath), "text/html; charset=utf-8")
        : Results.Content($"<h1>Demo page missing</h1><pre>{string.Join("\n", candidates)}</pre>", "text/html");
});

var products = app.MapGroup("/api/v1/products").WithTags("Products");
products.MapProductEndpoints();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
    await db.Database.EnsureCreatedAsync();

    if (!db.Products.Any())
    {
        var pizza = Product.Create("Margherita Pizza", "Classic tomato and mozzarella", 12.50m, 50);
        var burger = Product.Create("Cheese Burger", "Beef patty with cheddar", 9.90m, 80);
        db.Products.AddRange(pizza, burger);
        await db.SaveChangesAsync();

        var readStore = scope.ServiceProvider.GetRequiredService<ProductReadStore>();
        foreach (var product in new[] { pizza, burger })
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
}

app.Run();

public partial class Program;
