using FluentValidation;
using FoodDelivery.BuildingBlocks.CQRS;
using FoodDelivery.BuildingBlocks.Messaging;
using FoodDelivery.Services.Orders.Api.Data;
using FoodDelivery.Services.Orders.Api.Domain;
using Wolverine;

namespace FoodDelivery.Services.Orders.Api.Features.Orders;

public sealed record CreateOrderLineDto(Guid ProductId, string ProductName, int Quantity, decimal UnitPrice);

public sealed record CreateOrderCommand(Guid CustomerId, IReadOnlyList<CreateOrderLineDto> Lines)
    : ICommand<CreateOrderResult>;

public sealed record CreateOrderResult(Guid OrderId, decimal TotalAmount);

public sealed record GetOrdersQuery : IQuery<IReadOnlyList<OrderDto>>;
public sealed record GetOrderByIdQuery(Guid Id) : IQuery<OrderDto?>;
public sealed record OrderDto(
    Guid Id,
    Guid CustomerId,
    string Status,
    decimal TotalAmount,
    DateTime CreatedAtUtc,
    IReadOnlyList<CreateOrderLineDto> Lines);

public sealed class CreateOrderValidator : AbstractValidator<CreateOrderCommand>
{
    public CreateOrderValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.Lines).NotEmpty();
        RuleForEach(x => x.Lines).ChildRules(line =>
        {
            line.RuleFor(x => x.ProductId).NotEmpty();
            line.RuleFor(x => x.ProductName).NotEmpty();
            line.RuleFor(x => x.Quantity).GreaterThan(0);
            line.RuleFor(x => x.UnitPrice).GreaterThan(0);
        });
    }
}

public sealed class CreateOrderHandler(OrdersDbContext db, OrderReadStore readStore, IMessageBus bus)
    : ICommandHandler<CreateOrderCommand, CreateOrderResult>
{
    public async Task<CreateOrderResult> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        var lines = request.Lines
            .Select(x => OrderLine.Create(x.ProductId, x.ProductName, x.Quantity, x.UnitPrice))
            .ToList();

        var order = Order.Create(request.CustomerId, lines);
        db.Orders.Add(order);
        await db.SaveChangesAsync(cancellationToken);

        var readModel = new OrderReadModel
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
        };

        await readStore.UpsertAsync(readModel, cancellationToken);

        await bus.PublishAsync(new OrderCreatedIntegrationEvent(
            order.Id,
            order.CustomerId,
            order.TotalAmount,
            order.Lines.Select(x => new OrderLineIntegrationDto(x.ProductId, x.ProductName, x.Quantity, x.UnitPrice)).ToList()));

        return new CreateOrderResult(order.Id, order.TotalAmount);
    }
}

public sealed class GetOrdersHandler(OrderReadStore readStore)
    : IQueryHandler<GetOrdersQuery, IReadOnlyList<OrderDto>>
{
    public async Task<IReadOnlyList<OrderDto>> Handle(GetOrdersQuery request, CancellationToken cancellationToken)
    {
        var items = await readStore.ListAsync(cancellationToken);
        return items.Select(Map).ToList();
    }

    internal static OrderDto Map(OrderReadModel x) =>
        new(x.Id, x.CustomerId, x.Status, x.TotalAmount, x.CreatedAtUtc,
            x.Lines.Select(l => new CreateOrderLineDto(l.ProductId, l.ProductName, l.Quantity, l.UnitPrice)).ToList());
}

public sealed class GetOrderByIdHandler(OrderReadStore readStore)
    : IQueryHandler<GetOrderByIdQuery, OrderDto?>
{
    public async Task<OrderDto?> Handle(GetOrderByIdQuery request, CancellationToken cancellationToken)
    {
        var item = await readStore.GetAsync(request.Id, cancellationToken);
        return item is null ? null : GetOrdersHandler.Map(item);
    }
}

public static class OrderEndpoints
{
    public static RouteGroupBuilder MapOrderEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/", async (CreateOrderCommand command, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(command, ct);
            return Results.Created($"/api/v1/orders/{result.OrderId}", result);
        });

        group.MapGet("/", async (IMediator mediator, CancellationToken ct) =>
            Results.Ok(await mediator.Send(new GetOrdersQuery(), ct)));

        group.MapGet("/{id:guid}", async (Guid id, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetOrderByIdQuery(id), ct);
            return result is null ? Results.NotFound() : Results.Ok(result);
        });

        return group;
    }
}
