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

    public Task DeleteAsync(Guid id, CancellationToken ct = default) =>
        Collection.DeleteOneAsync(x => x.Id == id, ct);

    private IMongoCollection<ProcessedMarker> Markers =>
        client.GetDatabase("food_delivery_catalogs").GetCollection<ProcessedMarker>("processed_events");

    public async Task<bool> TryMarkProcessedAsync(string markerId, CancellationToken ct = default)
    {
        try
        {
            await Markers.InsertOneAsync(
                new ProcessedMarker { Id = markerId, ProcessedAtUtc = DateTime.UtcNow },
                cancellationToken: ct);
            return true;
        }
        catch (MongoWriteException ex) when (ex.WriteError?.Category == ServerErrorCategory.DuplicateKey)
        {
            return false;
        }
    }

    private sealed class ProcessedMarker
    {
        public string Id { get; set; } = default!;
        public DateTime ProcessedAtUtc { get; set; }
    }
}
