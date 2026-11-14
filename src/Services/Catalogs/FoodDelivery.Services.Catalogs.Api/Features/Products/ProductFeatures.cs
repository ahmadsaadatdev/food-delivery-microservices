using FluentValidation;
using FoodDelivery.BuildingBlocks.CQRS;
using FoodDelivery.BuildingBlocks.Messaging;
using FoodDelivery.Services.Catalogs.Api.Data;
using FoodDelivery.Services.Catalogs.Api.Domain;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Wolverine;

namespace FoodDelivery.Services.Catalogs.Api.Features.Products;

public sealed record CreateProductCommand(string Name, string Description, decimal Price, int Stock)
    : ICommand<CreateProductResult>;

public sealed record CreateProductResult(Guid Id);

public sealed record GetProductsQuery : IQuery<IReadOnlyList<ProductDto>>;
public sealed record GetProductByIdQuery(Guid Id) : IQuery<ProductDto?>;
public sealed record ProductDto(Guid Id, string Name, string Description, decimal Price, int Stock);

public sealed class CreateProductValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(2000);
        RuleFor(x => x.Price).GreaterThan(0);
        RuleFor(x => x.Stock).GreaterThanOrEqualTo(0);
    }
}

public sealed class CreateProductHandler(
    CatalogDbContext db,
    ProductReadStore readStore,
    IMessageBus bus,
    IMemoryCache cache,
    IOutputCacheStore outputCache) : ICommandHandler<CreateProductCommand, CreateProductResult>
{
    public async Task<CreateProductResult> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        var product = Product.Create(request.Name, request.Description, request.Price, request.Stock);
        db.Products.Add(product);
        await db.SaveChangesAsync(cancellationToken);

        await readStore.UpsertAsync(new ProductReadModel
        {
            Id = product.Id,
            Name = product.Name,
            Description = product.Description,
            Price = product.Price,
            Stock = product.Stock
        }, cancellationToken);

        await ProductCache.InvalidateAsync(cache, outputCache, cancellationToken);

        await bus.PublishAsync(new ProductCreatedIntegrationEvent(
            product.Id, product.Name, product.Description, product.Price, product.Stock));

        return new CreateProductResult(product.Id);
    }
}

public sealed class GetProductsHandler(ProductReadStore readStore, IMemoryCache cache)
    : IQueryHandler<GetProductsQuery, IReadOnlyList<ProductDto>>
{
    public async Task<IReadOnlyList<ProductDto>> Handle(GetProductsQuery request, CancellationToken cancellationToken)
    {
        return await cache.GetOrCreateAsync(ProductCache.ListKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(30);
            var items = await readStore.ListAsync(cancellationToken);
            return (IReadOnlyList<ProductDto>)items
                .Select(x => new ProductDto(x.Id, x.Name, x.Description, x.Price, x.Stock))
                .ToList();
        }) ?? [];
    }
}

public sealed class GetProductByIdHandler(ProductReadStore readStore)
    : IQueryHandler<GetProductByIdQuery, ProductDto?>
{
    public async Task<ProductDto?> Handle(GetProductByIdQuery request, CancellationToken cancellationToken)
    {
        var item = await readStore.GetAsync(request.Id, cancellationToken);
        return item is null
            ? null
            : new ProductDto(item.Id, item.Name, item.Description, item.Price, item.Stock);
    }
}

// Same-service create already upserts Mongo; keep this handler for idempotent redelivery only.
public sealed class ProductCreatedIntegrationEventHandler(
    ProductReadStore readStore,
    IMemoryCache cache,
    IOutputCacheStore outputCache)
{
    public async Task Handle(ProductCreatedIntegrationEvent message)
    {
        await readStore.UpsertAsync(new ProductReadModel
        {
            Id = message.ProductId,
            Name = message.Name,
            Description = message.Description,
            Price = message.Price,
            Stock = message.Stock
        });
        await ProductCache.InvalidateAsync(cache, outputCache);
    }
}

public sealed class OrderCreatedIntegrationEventHandler(
    CatalogDbContext db,
    ProductReadStore readStore,
    IMemoryCache cache,
    IOutputCacheStore outputCache)
{
    public async Task Handle(OrderCreatedIntegrationEvent message)
    {
        // Claim the order first so redelivery cannot double-decrement stock.
        var markerId = $"order-stock:{message.OrderId:N}";
        if (!await readStore.TryMarkProcessedAsync(markerId))
        {
            return;
        }

        var productIds = message.Lines.Select(x => x.ProductId).Distinct().ToArray();
        if (productIds.Length == 0)
        {
            return;
        }

        // One round-trip instead of N+1 queries per order line.
        var products = await db.Products
            .AsTracking()
            .Where(x => productIds.Contains(x.Id))
            .ToListAsync();

        if (products.Count == 0)
        {
            return;
        }

        var quantityByProduct = message.Lines
            .GroupBy(x => x.ProductId)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Quantity));

        var readModels = new List<ProductReadModel>(products.Count);
        foreach (var product in products)
        {
            if (!quantityByProduct.TryGetValue(product.Id, out var qty))
            {
                continue;
            }

            product.UpdateStock(Math.Max(0, product.Stock - qty));
            readModels.Add(new ProductReadModel
            {
                Id = product.Id,
                Name = product.Name,
                Description = product.Description,
                Price = product.Price,
                Stock = product.Stock
            });
        }

        await db.SaveChangesAsync();
        await readStore.BulkUpsertAsync(readModels);
        await ProductCache.InvalidateAsync(cache, outputCache);
    }
}

internal static class ProductCache
{
    public const string ListKey = "catalogs:products:list";

    public static async Task InvalidateAsync(
        IMemoryCache cache,
        IOutputCacheStore outputCache,
        CancellationToken ct = default)
    {
        cache.Remove(ListKey);
        await outputCache.EvictByTagAsync("products", ct);
    }
}

public static class ProductEndpoints
{
    public static RouteGroupBuilder MapProductEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/", async (CreateProductCommand command, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(command, ct);
            return Results.Created($"/api/v1/products/{result.Id}", result);
        });

        group.MapGet("/", async (IMediator mediator, CancellationToken ct) =>
            Results.Ok(await mediator.Send(new GetProductsQuery(), ct)))
            .CacheOutput("products");

        group.MapGet("/{id:guid}", async (Guid id, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetProductByIdQuery(id), ct);
            return result is null ? Results.NotFound() : Results.Ok(result);
        })
        .CacheOutput("products");

        return group;
    }
}
