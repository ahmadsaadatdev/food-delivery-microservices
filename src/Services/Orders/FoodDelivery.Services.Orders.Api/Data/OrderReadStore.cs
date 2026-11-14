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

public sealed class OrderReadStore
{
    private readonly Lazy<IMongoCollection<OrderReadModel>> _collection;

    public OrderReadStore(IMongoClient client)
    {
        _collection = new(() =>
            client.GetDatabase("food_delivery_orders").GetCollection<OrderReadModel>("orders_read"));
    }

    private IMongoCollection<OrderReadModel> Collection => _collection.Value;

    public async Task EnsureIndexesAsync(CancellationToken ct = default)
    {
        var indexes = new[]
        {
            new CreateIndexModel<OrderReadModel>(
                Builders<OrderReadModel>.IndexKeys.Ascending(x => x.Id),
                new CreateIndexOptions { Unique = true, Name = "ux_orders_read_id" }),
            new CreateIndexModel<OrderReadModel>(
                Builders<OrderReadModel>.IndexKeys.Descending(x => x.CreatedAtUtc),
                new CreateIndexOptions { Name = "ix_orders_read_created" }),
            new CreateIndexModel<OrderReadModel>(
                Builders<OrderReadModel>.IndexKeys.Ascending(x => x.CustomerId),
                new CreateIndexOptions { Name = "ix_orders_read_customer" })
        };

        await Collection.Indexes.CreateManyAsync(indexes, cancellationToken: ct);
    }

    public Task UpsertAsync(OrderReadModel model, CancellationToken ct = default) =>
        Collection.ReplaceOneAsync(
            x => x.Id == model.Id,
            model,
            new ReplaceOptions { IsUpsert = true },
            ct);

    public async Task<OrderReadModel?> GetAsync(Guid id, CancellationToken ct = default) =>
        await Collection.Find(x => x.Id == id).Limit(1).FirstOrDefaultAsync(ct);

    public async Task<IReadOnlyList<OrderReadModel>> ListAsync(CancellationToken ct = default) =>
        await Collection.Find(FilterDefinition<OrderReadModel>.Empty)
            .SortByDescending(x => x.CreatedAtUtc)
            .ToListAsync(ct);

    public Task DeleteAsync(Guid id, CancellationToken ct = default) =>
        Collection.DeleteOneAsync(x => x.Id == id, ct);
}