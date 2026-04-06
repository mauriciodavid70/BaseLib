using Microsoft.Extensions.DependencyInjection;
using NATS.Client.Core;
using NATS.Client.JetStream;

namespace BaseLib.Core.Services.Nats
{
    /// <summary>
    /// <see cref="IServiceCollection"/> extension methods for registering the NATS JetStream
    /// transport implementations of <see cref="ICoreServiceFireOnly"/> and <see cref="ICoreStatusEventSink"/>.
    /// </summary>
    public static class NatsTransportExtensions
    {
        /// <summary>
        /// Registers <see cref="NatsCoreServiceFireOnly"/> as <see cref="ICoreServiceFireOnly"/>
        /// and <see cref="NatsCoreStatusEventSink"/> as <see cref="ICoreStatusEventSink"/>.
        /// Both are registered as singletons. The caller is responsible for registering
        /// <see cref="INatsJSContext"/> and <see cref="INatsConnection"/> in the container
        /// before calling this method.
        /// </summary>
        /// <param name="services">The service collection to configure.</param>
        /// <param name="configure">Delegate to configure <see cref="NatsTransportOptions"/>.</param>
        /// <returns>The same <see cref="IServiceCollection"/> for chaining.</returns>
        public static IServiceCollection AddNatsTransport(
            this IServiceCollection services,
            Action<NatsTransportOptions> configure)
        {
            var options = new NatsTransportOptions();
            configure(options);

            services.AddSingleton(options);
            services.AddSingleton<ICoreServiceFireOnly>(sp =>
                new NatsCoreServiceFireOnly(
                    sp.GetRequiredService<INatsJSContext>(),
                    sp.GetRequiredService<NatsTransportOptions>()));

            services.AddSingleton<ICoreStatusEventSink>(sp =>
                new NatsCoreStatusEventSink(
                    sp.GetRequiredService<INatsConnection>()));

            return services;
        }
    }
}
