using System.Reflection;
using FluentValidation;
using FoodDelivery.BuildingBlocks.Auth;
using FoodDelivery.BuildingBlocks.CQRS;
using FoodDelivery.BuildingBlocks.Web;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDelivery.BuildingBlocks;

public static class DependencyInjection
{
    public static IServiceCollection AddBuildingBlocks(
        this IServiceCollection services,
        params Assembly[] assemblies)
    {
        var scanAssemblies = assemblies.Length == 0
            ? [Assembly.GetCallingAssembly()]
            : assemblies;

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblies(scanAssemblies));
        services.AddValidatorsFromAssemblies(scanAssemblies);
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddSingleton<JwtTokenService>();
        services.AddFoodDeliveryPerformance();

        return services;
    }
}
