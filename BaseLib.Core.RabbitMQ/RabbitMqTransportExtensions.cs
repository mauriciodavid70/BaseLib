using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;

namespace BaseLib.Core.Services.RabbitMQ
{
    /// <summary>
    /// <see cref="IServiceCollection"/> extension methods for registering the RabbitMQ
    /// transport implementations of <see cref="ICoreServiceFireOnly"/> and <see cref="ICoreStatusEventSink"/>.
    /// </summary>
    public static class RabbitMqTransportExtensions
    {
        /// <summary>
        /// Registers <see cref="RabbitMqCoreServiceFireOnly"/> as <see cref="ICoreServiceFireOnly"/>
        /// and <see cref="RabbitMqCoreStatusEventSink"/> as <see cref="ICoreStatusEventSink"/>.
        /// Both are registered as singletons. The caller is responsible for registering
        /// <see cref="IConnection"/> in the container before calling this method.
        /// </summary>
        /// <param name="services">The service collection to configure.</param>
        /// <param name="configure">Optional delegate to customise <see cref="RabbitMqOptions"/>.</param>
        /// <returns>The same <see cref="IServiceCollection"/> for chaining.</returns>
        public static IServiceCollection AddRabbitMqTransport(
            this IServiceCollection services,
            Action<RabbitMqOptions>? configure = null)
        {
            var options = new RabbitMqOptions();
            configure?.Invoke(options);

            services.AddSingleton(options);
            services.AddSingleton<ICoreServiceFireOnly>(sp =>
                new RabbitMqCoreServiceFireOnly(
                    sp.GetRequiredService<IConnection>(),
                    sp.GetRequiredService<RabbitMqOptions>()));

            services.AddSingleton<ICoreStatusEventSink>(sp =>
                new RabbitMqCoreStatusEventSink(
                    sp.GetRequiredService<IConnection>(),
                    sp.GetRequiredService<RabbitMqOptions>()));

            return services;
        }
    }
}
