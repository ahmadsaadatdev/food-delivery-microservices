using MongoDB.Driver;

namespace FoodDelivery.Services.Customers.Api.Data;

public sealed class CustomerReadModel
{
    public Guid Id { get; set; }
    public Guid IdentityId { get; set; }
    public string Email { get; set; } = default!;
    public string FullName { get; set; } = default!;
    public string? Phone { get; set; }
}

public sealed class CustomerReadStore
{
    private readonly Lazy<IMongoCollection<CustomerReadModel>> _collection;

    public CustomerReadStore(IMongoClient client)
    {
        _collection = new(() =>
            client.GetDatabase("food_delivery_customers").GetCollection<CustomerReadModel>("customers_read"));
    }

    private IMongoCollection<CustomerReadModel> Collection => _collection.Value;

    public async Task EnsureIndexesAsync(CancellationToken ct = default)
    {
        var indexes = new[]
        {
            new CreateIndexModel<CustomerReadModel>(
                Builders<CustomerReadModel>.IndexKeys.Ascending(x => x.Id),
                new CreateIndexOptions { Unique = true, Name = "ux_customers_read_id" }),
            new CreateIndexModel<CustomerReadModel>(
                Builders<CustomerReadModel>.IndexKeys.Ascending(x => x.IdentityId),
                new CreateIndexOptions { Unique = true, Name = "ux_customers_read_identity" }),
            new CreateIndexModel<CustomerReadModel>(
                Builders<CustomerReadModel>.IndexKeys.Ascending(x => x.Email),
                new CreateIndexOptions { Unique = true, Name = "ux_customers_read_email" })
        };

        await Collection.Indexes.CreateManyAsync(indexes, cancellationToken: ct);
    }

    public Task UpsertAsync(CustomerReadModel model, CancellationToken ct = default) =>
        Collection.ReplaceOneAsync(
            x => x.Id == model.Id,
            model,
            new ReplaceOptions { IsUpsert = true },
            ct);

    public async Task<CustomerReadModel?> GetAsync(Guid id, CancellationToken ct = default) =>
        await Collection.Find(x => x.Id == id).Limit(1).FirstOrDefaultAsync(ct);

    public async Task<IReadOnlyList<CustomerReadModel>> ListAsync(CancellationToken ct = default) =>
        await Collection.Find(FilterDefinition<CustomerReadModel>.Empty).ToListAsync(ct);

    public Task DeleteAsync(Guid id, CancellationToken ct = default) =>
        Collection.DeleteOneAsync(x => x.Id == id, ct);
}
