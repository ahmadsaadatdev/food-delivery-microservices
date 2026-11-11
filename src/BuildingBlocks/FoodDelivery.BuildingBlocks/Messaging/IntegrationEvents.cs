using FoodDelivery.BuildingBlocks.Domain;

namespace FoodDelivery.BuildingBlocks.Messaging;

public sealed record UserRegisteredIntegrationEvent(
    Guid UserId,
    string Email,
    string FirstName,
    string LastName) : IntegrationEvent;

public sealed record ProductCreatedIntegrationEvent(
    Guid ProductId,
    string Name,
    decimal Price,
    int Stock) : IntegrationEvent;

public sealed record OrderCreatedIntegrationEvent(
    Guid OrderId,
    Guid CustomerId,
    decimal TotalAmount,
    IReadOnlyList<OrderLineIntegrationDto> Lines) : IntegrationEvent;

public sealed record OrderLineIntegrationDto(
    Guid ProductId,
    string ProductName,
    int Quantity,
    decimal UnitPrice);
