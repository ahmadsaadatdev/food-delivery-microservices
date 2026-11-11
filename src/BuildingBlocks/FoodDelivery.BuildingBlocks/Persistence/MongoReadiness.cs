using MongoDB.Bson;
using MongoDB.Driver;

namespace FoodDelivery.BuildingBlocks.Persistence;

public static class MongoReadiness
{
    public static async Task WaitAsync(IMongoClient client, CancellationToken cancellationToken = default)
    {
        Exception? last = null;

        for (var attempt = 1; attempt <= 30; attempt++)
        {
            try
            {
                await client
                    .GetDatabase("admin")
                    .RunCommandAsync((Command<BsonDocument>)"{ping:1}", cancellationToken: cancellationToken);
                return;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                last = ex;
                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
            }
        }

        throw new InvalidOperationException("MongoDB did not become ready in time.", last);
    }
}
