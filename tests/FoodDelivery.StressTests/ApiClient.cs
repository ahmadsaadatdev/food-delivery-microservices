using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FoodDelivery.StressTests;

internal sealed class ApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly HttpClient _http;

    public ApiClient(HttpClient http) => _http = http;

    public Task<HttpResponseMessage> GetProductsAsync(CancellationToken ct = default) =>
        _http.GetAsync($"{StressConfig.CatalogsBase}/api/v1/products", ct);

    public Task<HttpResponseMessage> GetCustomersAsync(CancellationToken ct = default) =>
        _http.GetAsync($"{StressConfig.CustomersBase}/api/v1/customers", ct);

    public Task<HttpResponseMessage> GetOrdersAsync(CancellationToken ct = default) =>
        _http.GetAsync($"{StressConfig.OrdersBase}/api/v1/orders", ct);

    public Task<HttpResponseMessage> RegisterAsync(object body, CancellationToken ct = default) =>
        _http.PostAsJsonAsync($"{StressConfig.IdentityBase}/api/v1/identity/register", body, JsonOptions, ct);

    public Task<HttpResponseMessage> CreateOrderAsync(object body, CancellationToken ct = default) =>
        _http.PostAsJsonAsync($"{StressConfig.OrdersBase}/api/v1/orders", body, JsonOptions, ct);

    public async Task<T?> ReadJsonAsync<T>(HttpResponseMessage response, CancellationToken ct = default)
    {
        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        return await JsonSerializer.DeserializeAsync<T>(stream, JsonOptions, ct);
    }

    public static StringContent JsonContent(object body) =>
        new(JsonSerializer.Serialize(body, JsonOptions), Encoding.UTF8, "application/json");
}

internal sealed record ProductDto(Guid Id, string Name, string Description, decimal Price, int Stock);
internal sealed record CustomerDto(Guid Id, Guid IdentityId, string Email, string FullName, string? Phone);
internal sealed record RegisterResult(Guid UserId, string Email, string AccessToken);
internal sealed record CreateOrderResult(Guid OrderId, decimal TotalAmount);
