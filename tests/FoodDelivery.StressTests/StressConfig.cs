namespace FoodDelivery.StressTests;

internal static class StressConfig
{
    public static string CatalogsBase { get; } = Env("FD_CATALOGS_URL", "http://127.0.0.1:5100");
    public static string CustomersBase { get; } = Env("FD_CUSTOMERS_URL", "http://127.0.0.1:5200");
    public static string OrdersBase { get; } = Env("FD_ORDERS_URL", "http://127.0.0.1:5300");
    public static string IdentityBase { get; } = Env("FD_IDENTITY_URL", "http://127.0.0.1:5400");

    /// <summary>Duration of each stress scenario in seconds.</summary>
    public static int DurationSeconds { get; } = EnvInt("FD_STRESS_DURATION", 45);

    /// <summary>Target requests per second for read-heavy scenarios.</summary>
    public static int ReadRps { get; } = EnvInt("FD_STRESS_READ_RPS", 40);

    /// <summary>Target requests per second for write / end-to-end scenarios.</summary>
    public static int WriteRps { get; } = EnvInt("FD_STRESS_WRITE_RPS", 8);

    /// <summary>Concurrent copies for constant-load simulations.</summary>
    public static int Copies { get; } = EnvInt("FD_STRESS_COPIES", 20);

    private static string Env(string key, string fallback) =>
        Environment.GetEnvironmentVariable(key) is { Length: > 0 } v ? v.TrimEnd('/') : fallback;

    private static int EnvInt(string key, int fallback) =>
        int.TryParse(Environment.GetEnvironmentVariable(key), out var n) && n > 0 ? n : fallback;
}
