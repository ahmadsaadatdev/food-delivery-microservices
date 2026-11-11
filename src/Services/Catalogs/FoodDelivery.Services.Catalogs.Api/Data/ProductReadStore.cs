using MongoDB.Driver;

namespace FoodDelivery.Services.Catalogs.Api.Data;

public sealed class ProductReadModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
    public string Description { get; set; } = default!;
    public decimal Price { get; set; }
    public int Stock { get; set; }
}

public sealed class ProductReadStore(IMongoClient client)
{
    private IMongoCollection<ProductReadModel> Collection =>
        client.GetDatabase("food_delivery_catalogs").GetCollection<ProductReadModel>("products_read");

    public Task UpsertAsync(ProductReadModel model, CancellationToken ct = default) =>
        Collection.ReplaceOneAsync(
            x => x.Id == model.Id,
            model,
            new ReplaceOptions { IsUpsert = true },
            ct);

    public async Task<ProductReadModel?> GetAsync(Guid id, CancellationToken ct = default) =>
        await Collection.Find(x => x.Id == id).FirstOrDefaultAsync(ct);

    public async Task<IReadOnlyList<ProductReadModel>> ListAsync(CancellationToken ct = default) =>
        await Collection.Find(FilterDefinition<ProductReadModel>.Empty).ToListAsync(ct);
}
