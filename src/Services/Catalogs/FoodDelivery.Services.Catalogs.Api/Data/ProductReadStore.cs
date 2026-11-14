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

public sealed class ProductReadStore
{
    private readonly Lazy<IMongoCollection<ProductReadModel>> _collection;
    private readonly Lazy<IMongoCollection<ProcessedMarker>> _markers;

    public ProductReadStore(IMongoClient client)
    {
        _collection = new(() =>
            client.GetDatabase("food_delivery_catalogs").GetCollection<ProductReadModel>("products_read"));
        _markers = new(() =>
            client.GetDatabase("food_delivery_catalogs").GetCollection<ProcessedMarker>("processed_events"));
    }

    private IMongoCollection<ProductReadModel> Collection => _collection.Value;
    private IMongoCollection<ProcessedMarker> Markers => _markers.Value;

    public async Task EnsureIndexesAsync(CancellationToken ct = default)
    {
        // Id maps to Mongo _id (already unique). Index Name for catalog lookups.
        await Collection.Indexes.CreateOneAsync(
            new CreateIndexModel<ProductReadModel>(
                Builders<ProductReadModel>.IndexKeys.Ascending(x => x.Name),
                new CreateIndexOptions { Name = "ix_products_read_name" }),
            cancellationToken: ct);
    }

    public Task UpsertAsync(ProductReadModel model, CancellationToken ct = default) =>
        Collection.ReplaceOneAsync(
            x => x.Id == model.Id,
            model,
            new ReplaceOptions { IsUpsert = true },
            ct);

    public Task BulkUpsertAsync(IEnumerable<ProductReadModel> models, CancellationToken ct = default)
    {
        var writes = models
            .Select(model => new ReplaceOneModel<ProductReadModel>(
                Builders<ProductReadModel>.Filter.Eq(x => x.Id, model.Id),
                model)
            { IsUpsert = true })
            .ToList();

        if (writes.Count == 0)
        {
            return Task.CompletedTask;
        }

        return Collection.BulkWriteAsync(writes, cancellationToken: ct);
    }

    public async Task<ProductReadModel?> GetAsync(Guid id, CancellationToken ct = default) =>
        await Collection.Find(x => x.Id == id).Limit(1).FirstOrDefaultAsync(ct);

    public async Task<IReadOnlyList<ProductReadModel>> ListAsync(CancellationToken ct = default) =>
        await Collection.Find(FilterDefinition<ProductReadModel>.Empty)
            .Project(x => new ProductReadModel
            {
                Id = x.Id,
                Name = x.Name,
                Description = x.Description,
                Price = x.Price,
                Stock = x.Stock
            })
            .ToListAsync(ct);

    public Task DeleteAsync(Guid id, CancellationToken ct = default) =>
        Collection.DeleteOneAsync(x => x.Id == id, ct);

    public async Task DeleteManyAsync(IEnumerable<Guid> ids, CancellationToken ct = default)
    {
        var idList = ids as ICollection<Guid> ?? ids.ToList();
        if (idList.Count == 0)
        {
            return;
        }

        await Collection.DeleteManyAsync(x => idList.Contains(x.Id), ct);
    }

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
