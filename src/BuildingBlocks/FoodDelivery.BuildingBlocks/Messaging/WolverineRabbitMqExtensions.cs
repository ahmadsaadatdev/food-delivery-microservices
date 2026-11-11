using JasperFx.CodeGeneration.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Wolverine;
using Wolverine.RabbitMQ;

namespace FoodDelivery.BuildingBlocks.Messaging;

public static class WolverineRabbitMqExtensions
{
    public static IHostBuilder UseFoodDeliveryMessaging(
        this IHostBuilder host,
        IConfiguration configuration,
        string serviceQueueName,
        params System.Reflection.Assembly[] assemblies)
    {
        host.UseWolverine(opts =>
        {
            opts.UseRuntimeCompilation();
            opts.ServiceLocationPolicy = ServiceLocationPolicy.AlwaysAllowed;
            foreach (var assembly in assemblies)
            {
                opts.Discovery.IncludeAssembly(assembly);
            }

            var rabbit = configuration.GetConnectionString("rabbitmq");
            if (string.IsNullOrWhiteSpace(rabbit))
            {
                // Local queues only — integration events will not cross process boundaries.
                opts.PublishAllMessages().Locally();
                return;
            }

            opts.UseRabbitMq(new Uri(rabbit))
                .AutoProvision()
                .DeclareExchange("food-delivery", exchange =>
                {
                    exchange.BindQueue(serviceQueueName);
                });

            opts.PublishAllMessages().ToRabbitExchange("food-delivery");
            opts.ListenToRabbitQueue(serviceQueueName);
        });

        return host;
    }
}
