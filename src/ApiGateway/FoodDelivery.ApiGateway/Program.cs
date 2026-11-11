var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
    .AddServiceDiscoveryDestinationResolver();

builder.Services.AddOpenApi();

var app = builder.Build();

app.MapOpenApi();
app.MapDefaultEndpoints();
app.MapGet("/gateway-info", () => Results.Ok(new
{
    name = "FoodDelivery Api Gateway",
    routes = new[] { "/catalogs/{**}", "/customers/{**}", "/orders/{**}", "/identity/{**}" }
}));
app.MapReverseProxy();

app.Run();
