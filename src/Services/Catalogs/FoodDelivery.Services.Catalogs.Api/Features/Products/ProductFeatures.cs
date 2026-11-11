using FluentValidation;
using FoodDelivery.BuildingBlocks.CQRS;
using FoodDelivery.BuildingBlocks.Messaging;
using FoodDelivery.Services.Catalogs.Api.Data;
using FoodDelivery.Services.Catalogs.Api.Domain;
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

public sealed class CreateProductHandler(CatalogDbContext db, ProductReadStore readStore, IMessageBus bus)
    : ICommandHandler<CreateProductCommand, CreateProductResult>
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

        await bus.PublishAsync(new ProductCreatedIntegrationEvent(
            product.Id, product.Name, product.Price, product.Stock));

        return new CreateProductResult(product.Id);
    }
}

public sealed class GetProductsHandler(ProductReadStore readStore)
    : IQueryHandler<GetProductsQuery, IReadOnlyList<ProductDto>>
{
    public async Task<IReadOnlyList<ProductDto>> Handle(GetProductsQuery request, CancellationToken cancellationToken)
    {
        var items = await readStore.ListAsync(cancellationToken);
        return items.Select(x => new ProductDto(x.Id, x.Name, x.Description, x.Price, x.Stock)).ToList();
    }
}

public sealed class GetProductByIdHandler(ProductReadStore readStore)
    : IQueryHandler<GetProductByIdQuery, ProductDto?>
{
    public async Task<ProductDto?> Handle(GetProductByIdQuery request, CancellationToken cancellationToken)
    {
        var item = await readStore.GetAsync(request.Id, cancellationToken);
        return item is null ? null : new ProductDto(item.Id, item.Name, item.Description, item.Price, item.Stock);
    }
}

public sealed class ProductCreatedIntegrationEventHandler(ProductReadStore readStore)
{
    public Task Handle(ProductCreatedIntegrationEvent message) =>
        readStore.UpsertAsync(new ProductReadModel
        {
            Id = message.ProductId,
            Name = message.Name,
            Description = string.Empty,
            Price = message.Price,
            Stock = message.Stock
        });
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
            Results.Ok(await mediator.Send(new GetProductsQuery(), ct)));

        group.MapGet("/{id:guid}", async (Guid id, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetProductByIdQuery(id), ct);
            return result is null ? Results.NotFound() : Results.Ok(result);
        });

        return group;
    }
}
