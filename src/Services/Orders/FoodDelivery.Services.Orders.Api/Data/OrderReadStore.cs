using MongoDB.Driver;

namespace FoodDelivery.Services.Orders.Api.Data;

public sealed class OrderReadModel
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public string Status { get; set; } = default!;
    public decimal TotalAmount { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public List<OrderLineReadModel> Lines { get; set; } = [];
}

public sealed class OrderLineReadModel
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = default!;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}

public sealed class OrderReadStore(IMongoClient client)
{
    private IMongoCollection<OrderReadModel> Collection =>
        client.GetDatabase("food_delivery_orders").GetCollection<OrderReadModel>("orders_read");

    public Task UpsertAsync(OrderReadModel model, CancellationToken ct = default) =>
        Collection.ReplaceOneAsync(
            x => x.Id == model.Id,
            model,
            new ReplaceOptions { IsUpsert = true },
            ct);

    public async Task<OrderReadModel?> GetAsync(Guid id, CancellationToken ct = default) =>
        await Collection.Find(x => x.Id == id).FirstOrDefaultAsync(ct);

    public async Task<IReadOnlyList<OrderReadModel>> ListAsync(CancellationToken ct = default) =>
        await Collection.Find(FilterDefinition<OrderReadModel>.Empty).SortByDescending(x => x.CreatedAtUtc).ToListAsync(ct);
}
