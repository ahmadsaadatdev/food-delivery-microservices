using System.Net.Http.Json;
using FoodDelivery.StressTests;
using NBomber.Contracts;
using NBomber.Contracts.Stats;
using NBomber.CSharp;

Console.WriteLine("Food Delivery — Stress Tests");
Console.WriteLine($"Catalogs : {StressConfig.CatalogsBase}");
Console.WriteLine($"Customers: {StressConfig.CustomersBase}");
Console.WriteLine($"Orders   : {StressConfig.OrdersBase}");
Console.WriteLine($"Identity : {StressConfig.IdentityBase}");
Console.WriteLine($"Duration : {StressConfig.DurationSeconds}s | ReadRPS={StressConfig.ReadRps} | WriteRPS={StressConfig.WriteRps}");
Console.WriteLine();

using var sharedHandler = new SocketsHttpHandler
{
    PooledConnectionLifetime = TimeSpan.FromMinutes(5),
    MaxConnectionsPerServer = 200,
    EnableMultipleHttp2Connections = true
};
using var sharedHttp = new HttpClient(sharedHandler)
{
    Timeout = TimeSpan.FromSeconds(30)
};

await EnsureApisHealthyAsync(sharedHttp);

var seedProducts = await sharedHttp.GetFromJsonAsync<List<ProductDto>>($"{StressConfig.CatalogsBase}/api/v1/products")
    ?? throw new InvalidOperationException("No products available for stress test seed.");
if (seedProducts.Count == 0)
{
    throw new InvalidOperationException("Product catalog is empty. Start Catalogs API and seed data first.");
}

var seedProduct = seedProducts[0];
Console.WriteLine($"Seed product: {seedProduct.Name} ({seedProduct.Id}) @ {seedProduct.Price}");
Console.WriteLine();

var duration = TimeSpan.FromSeconds(StressConfig.DurationSeconds);
var api = new ApiClient(sharedHttp);

var getProducts = Scenario.Create("get_products", async context =>
{
    var response = await api.GetProductsAsync();
    return ToStepResult(response);
})
.WithWarmUpDuration(TimeSpan.FromSeconds(3))
.WithLoadSimulations(
    Simulation.Inject(
        rate: StressConfig.ReadRps,
        interval: TimeSpan.FromSeconds(1),
        during: duration));

var getCustomers = Scenario.Create("get_customers", async context =>
{
    var response = await api.GetCustomersAsync();
    return ToStepResult(response);
})
.WithWarmUpDuration(TimeSpan.FromSeconds(3))
.WithLoadSimulations(
    Simulation.Inject(
        rate: Math.Max(5, StressConfig.ReadRps / 2),
        interval: TimeSpan.FromSeconds(1),
        during: duration));

var registerUsers = Scenario.Create("register_users", async context =>
{
    var email = $"stress_{context.InvocationNumber}_{Guid.NewGuid():N}@load.test";
    var response = await api.RegisterAsync(new
    {
        email,
        password = "Pass@12345",
        firstName = "Stress",
        lastName = $"User{context.InvocationNumber}"
    });
    return ToStepResult(response);
})
.WithWarmUpDuration(TimeSpan.FromSeconds(3))
.WithLoadSimulations(
    Simulation.Inject(
        rate: StressConfig.WriteRps,
        interval: TimeSpan.FromSeconds(1),
        during: duration));

var endToEndOrder = Scenario.Create("end_to_end_order", async context =>
{
    var email = $"e2e_{context.InvocationNumber}_{Guid.NewGuid():N}@load.test";

    var registerResponse = await api.RegisterAsync(new
    {
        email,
        password = "Pass@12345",
        firstName = "E2E",
        lastName = $"Buyer{context.InvocationNumber}"
    });

    if (!registerResponse.IsSuccessStatusCode)
    {
        return Response.Fail(statusCode: ((int)registerResponse.StatusCode).ToString(), message: "register failed");
    }

    var registered = await api.ReadJsonAsync<RegisterResult>(registerResponse);
    if (registered is null)
    {
        return Response.Fail(statusCode: "500", message: "register payload empty");
    }

    var customer = await WaitForCustomerAsync(api, registered.UserId, TimeSpan.FromSeconds(8));
    if (customer is null)
    {
        return Response.Fail(statusCode: "408", message: "customer projection timeout");
    }

    var productsResponse = await api.GetProductsAsync();
    if (!productsResponse.IsSuccessStatusCode)
    {
        return Response.Fail(statusCode: ((int)productsResponse.StatusCode).ToString(), message: "products failed");
    }

    var products = await api.ReadJsonAsync<List<ProductDto>>(productsResponse) ?? [];
    var product = products.FirstOrDefault() ?? seedProduct;

    var orderResponse = await api.CreateOrderAsync(new
    {
        customerId = customer.Id,
        lines = new[]
        {
            new
            {
                productId = product.Id,
                productName = product.Name,
                quantity = 1,
                unitPrice = product.Price
            }
        }
    });

    if (!orderResponse.IsSuccessStatusCode)
    {
        var err = await orderResponse.Content.ReadAsStringAsync();
        return Response.Fail(
            statusCode: ((int)orderResponse.StatusCode).ToString(),
            message: $"order failed: {Truncate(err, 120)}");
    }

    return Response.Ok(statusCode: "200", sizeBytes: 0);
})
.WithWarmUpDuration(TimeSpan.FromSeconds(5))
.WithLoadSimulations(
    Simulation.Inject(
        rate: Math.Max(2, StressConfig.WriteRps / 2),
        interval: TimeSpan.FromSeconds(1),
        during: duration));

var mixedKeepAlive = Scenario.Create("mixed_read_burst", async context =>
{
    var p = await api.GetProductsAsync();
    var c = await api.GetCustomersAsync();
    var o = await api.GetOrdersAsync();

    if (!p.IsSuccessStatusCode || !c.IsSuccessStatusCode || !o.IsSuccessStatusCode)
    {
        return Response.Fail(
            statusCode: "multi",
            message: $"p={(int)p.StatusCode},c={(int)c.StatusCode},o={(int)o.StatusCode}");
    }

    return Response.Ok(statusCode: "200", sizeBytes: 0);
})
.WithWarmUpDuration(TimeSpan.FromSeconds(3))
.WithLoadSimulations(
    Simulation.KeepConstant(copies: Math.Min(10, StressConfig.Copies), during: duration));

var reportPath = Path.Combine(
    AppContext.BaseDirectory,
    "stress-reports",
    $"report-{DateTime.UtcNow:yyyyMMdd-HHmmss}");
Directory.CreateDirectory(reportPath);

var stats = NBomberRunner
    .RegisterScenarios(getProducts, getCustomers, registerUsers, endToEndOrder, mixedKeepAlive)
    .WithReportFolder(reportPath)
    .WithReportFileName("food-delivery-stress")
    .Run();

Console.WriteLine();
Console.WriteLine($"HTML report: {Path.GetFullPath(reportPath)}");
PrintSummary(stats);
Console.WriteLine("Done.");

return 0;

static Response<object> ToStepResult(HttpResponseMessage response)
{
    var code = ((int)response.StatusCode).ToString();
    return response.IsSuccessStatusCode
        ? Response.Ok(statusCode: code, sizeBytes: response.Content.Headers.ContentLength ?? 0)
        : Response.Fail(statusCode: code, message: response.ReasonPhrase ?? "failed");
}

static async Task EnsureApisHealthyAsync(HttpClient http)
{
    var targets = new (string Name, string Url)[]
    {
        ("Catalogs", $"{StressConfig.CatalogsBase}/"),
        ("Customers", $"{StressConfig.CustomersBase}/"),
        ("Orders", $"{StressConfig.OrdersBase}/"),
        ("Identity", $"{StressConfig.IdentityBase}/")
    };

    foreach (var (name, url) in targets)
    {
        try
        {
            using var res = await http.GetAsync(url);
            Console.WriteLine($"Health {name}: {(int)res.StatusCode}");
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Service '{name}' is not reachable at {url}. Start APIs before stress test. ({ex.Message})");
        }
    }
}

static async Task<CustomerDto?> WaitForCustomerAsync(ApiClient api, Guid identityId, TimeSpan timeout)
{
    var deadline = DateTime.UtcNow + timeout;
    while (DateTime.UtcNow < deadline)
    {
        var response = await api.GetCustomersAsync();
        if (response.IsSuccessStatusCode)
        {
            var customers = await api.ReadJsonAsync<List<CustomerDto>>(response) ?? [];
            var match = customers.FirstOrDefault(x => x.IdentityId == identityId);
            if (match is not null)
            {
                return match;
            }
        }

        await Task.Delay(200);
    }

    return null;
}

static void PrintSummary(NodeStats stats)
{
    Console.WriteLine();
    Console.WriteLine("=== SUMMARY ===");
    foreach (var sc in stats.ScenarioStats)
    {
        var ok = sc.Ok.Request.Count;
        var fail = sc.Fail.Request.Count;
        var total = ok + fail;
        var failPct = total == 0 ? 0 : fail * 100.0 / total;
        Console.WriteLine(
            $"{sc.ScenarioName,-22} ok={ok,6} fail={fail,6} fail%={failPct,5:F1}  " +
            $"p50={sc.Ok.Latency.Percent50:F0}ms p95={sc.Ok.Latency.Percent95:F0}ms rps={sc.Ok.Request.RPS:F1}");
    }
}

static string Truncate(string value, int max) =>
    value.Length <= max ? value : value[..max] + "...";
