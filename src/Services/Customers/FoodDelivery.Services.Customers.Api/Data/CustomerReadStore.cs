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

public sealed class CustomerReadStore(IMongoClient client)
{
    private IMongoCollection<CustomerReadModel> Collection =>
        client.GetDatabase("food_delivery_customers").GetCollection<CustomerReadModel>("customers_read");

    public Task UpsertAsync(CustomerReadModel model, CancellationToken ct = default) =>
        Collection.ReplaceOneAsync(
            x => x.Id == model.Id,
            model,
            new ReplaceOptions { IsUpsert = true },
            ct);

    public async Task<CustomerReadModel?> GetAsync(Guid id, CancellationToken ct = default) =>
        await Collection.Find(x => x.Id == id).FirstOrDefaultAsync(ct);

    public async Task<IReadOnlyList<CustomerReadModel>> ListAsync(CancellationToken ct = default) =>
        await Collection.Find(FilterDefinition<CustomerReadModel>.Empty).ToListAsync(ct);
}
