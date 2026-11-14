using System.IO.Compression;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDelivery.BuildingBlocks.Web;

public static class PerformanceExtensions
{
    public static IServiceCollection AddFoodDeliveryPerformance(this IServiceCollection services)
    {
        services.AddMemoryCache();
        services.AddResponseCompression(options =>
        {
            options.EnableForHttps = true;
            options.Providers.Add<BrotliCompressionProvider>();
            options.Providers.Add<GzipCompressionProvider>();
            options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
            [
                "application/json",
                "application/problem+json",
                "text/plain"
            ]);
        });
        services.Configure<BrotliCompressionProviderOptions>(o => o.Level = CompressionLevel.Fastest);
        services.Configure<GzipCompressionProviderOptions>(o => o.Level = CompressionLevel.Fastest);

        services.AddOutputCache(options =>
        {
            options.AddBasePolicy(b => b.Expire(TimeSpan.FromSeconds(15)));
            options.AddPolicy("products", b => b.Expire(TimeSpan.FromSeconds(30)).Tag("products"));
            options.AddPolicy("customers", b => b.Expire(TimeSpan.FromSeconds(30)).Tag("customers"));
            options.AddPolicy("orders", b => b.Expire(TimeSpan.FromSeconds(15)).Tag("orders"));
        });

        return services;
    }

    public static WebApplication UseFoodDeliveryPerformance(this WebApplication app)
    {
        app.UseResponseCompression();
        app.UseOutputCache();
        return app;
    }
}
