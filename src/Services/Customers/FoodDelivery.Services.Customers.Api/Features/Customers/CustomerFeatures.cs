using FluentValidation;
using FoodDelivery.BuildingBlocks.CQRS;
using FoodDelivery.BuildingBlocks.Messaging;
using FoodDelivery.Services.Customers.Api.Data;
using FoodDelivery.Services.Customers.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace FoodDelivery.Services.Customers.Api.Features.Customers;

public sealed record GetCustomersQuery : IQuery<IReadOnlyList<CustomerDto>>;
public sealed record GetCustomerByIdQuery(Guid Id) : IQuery<CustomerDto?>;
public sealed record CustomerDto(Guid Id, Guid IdentityId, string Email, string FullName, string? Phone);

public sealed class GetCustomersHandler(CustomerReadStore readStore)
    : IQueryHandler<GetCustomersQuery, IReadOnlyList<CustomerDto>>
{
    public async Task<IReadOnlyList<CustomerDto>> Handle(GetCustomersQuery request, CancellationToken cancellationToken)
    {
        var items = await readStore.ListAsync(cancellationToken);
        return items.Select(x => new CustomerDto(x.Id, x.IdentityId, x.Email, x.FullName, x.Phone)).ToList();
    }
}

public sealed class GetCustomerByIdHandler(CustomerReadStore readStore)
    : IQueryHandler<GetCustomerByIdQuery, CustomerDto?>
{
    public async Task<CustomerDto?> Handle(GetCustomerByIdQuery request, CancellationToken cancellationToken)
    {
        var item = await readStore.GetAsync(request.Id, cancellationToken);
        return item is null
            ? null
            : new CustomerDto(item.Id, item.IdentityId, item.Email, item.FullName, item.Phone);
    }
}

public sealed class UserRegisteredIntegrationEventHandler(CustomersDbContext db, CustomerReadStore readStore)
{
    public async Task Handle(UserRegisteredIntegrationEvent message)
    {
        if (await db.Customers.AnyAsync(x => x.IdentityId == message.UserId))
        {
            return;
        }

        var customer = Customer.CreateFromIdentity(message.UserId, message.Email, message.FirstName, message.LastName);
        db.Customers.Add(customer);
        await db.SaveChangesAsync();

        await readStore.UpsertAsync(new CustomerReadModel
        {
            Id = customer.Id,
            IdentityId = customer.IdentityId,
            Email = customer.Email,
            FullName = $"{customer.FirstName} {customer.LastName}"
        });
    }
}

public static class CustomerEndpoints
{
    public static RouteGroupBuilder MapCustomerEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/", async (IMediator mediator, CancellationToken ct) =>
            Results.Ok(await mediator.Send(new GetCustomersQuery(), ct)));

        group.MapGet("/{id:guid}", async (Guid id, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetCustomerByIdQuery(id), ct);
            return result is null ? Results.NotFound() : Results.Ok(result);
        });

        return group;
    }
}
