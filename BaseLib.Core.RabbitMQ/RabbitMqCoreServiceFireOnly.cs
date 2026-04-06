using System.Text;
using BaseLib.Core.Models;
using BaseLib.Core.Serialization;
using RabbitMQ.Client;

namespace BaseLib.Core.Services.RabbitMQ
{
    /// <summary>
    /// <see cref="ICoreServiceFireOnly"/> implementation that dispatches service invocations
    /// as messages to a durable RabbitMQ topic exchange. The routing key equals the
    /// assembly-qualified service type name, allowing consumers to bind queues with
    /// exact or wildcard patterns. The exchange is declared lazily on first use (thread-safe).
    /// Register as a singleton.
    /// </summary>
    public class RabbitMqCoreServiceFireOnly : ICoreServiceFireOnly
    {
        private static readonly BasicProperties EmptyProps = new();

        private readonly IConnection connection;
        private readonly RabbitMqOptions options;
        private bool isInitialized;
        private readonly SemaphoreSlim initLock = new(1, 1);

        /// <summary>Initializes the fire-only dispatcher.</summary>
        /// <param name="connection">Long-lived RabbitMQ connection (singleton).</param>
        /// <param name="options">Exchange name configuration.</param>
        public RabbitMqCoreServiceFireOnly(IConnection connection, RabbitMqOptions options)
        {
            this.connection = connection;
            this.options = options;
        }

        /// <inheritdoc/>
        public Task FireAsync<TService>(CoreRequestBase request, string? correlationId = null, bool isLongRunningChild = false)
            where TService : ICoreServiceBase
        {
            var type = typeof(TService);
            var typeName = $"{type.FullName}, {type.Assembly.GetName().Name}";
            return FireAsync(typeName, request, correlationId, isLongRunningChild);
        }

        /// <inheritdoc/>
        public async Task FireAsync(string typeName, CoreRequestBase request, string? correlationId = null, bool isLongRunningChild = false)
        {
            var message = new FireAsyncMessage
            {
                TypeName = typeName,
                Method = "RunAsync",
                Request = request,
                CorrelationId = correlationId,
                IsLongRunningChild = isLongRunningChild
            };

            var body = Encoding.UTF8.GetBytes(CoreSerializer.Serialize(message));

            await using var channel = await EnsureExchangeAndCreateChannelAsync();
            await channel.BasicPublishAsync(
                exchange: options.ExchangeName,
                routingKey: typeName,
                mandatory: false,
                basicProperties: EmptyProps,
                body: body);
        }

        /// <inheritdoc/>
        public Task FireManyAsync<TService>(IEnumerable<CoreRequestBase> requests, string? correlationId = null, bool isLongRunningChild = false)
            where TService : ICoreServiceBase
        {
            var type = typeof(TService);
            var typeName = $"{type.FullName}, {type.Assembly.GetName().Name}";
            return FireManyAsync(typeName, requests, correlationId, isLongRunningChild);
        }

        /// <summary>
        /// Dispatches multiple service invocations by type name to the RabbitMQ exchange using
        /// a single <see cref="IChannel"/> for the entire batch.
        /// </summary>
        /// <param name="typeName">Assembly-qualified type name of the target service.</param>
        /// <param name="requests">Collection of request payloads.</param>
        /// <param name="correlationId">Correlation ID written into each message envelope.</param>
        /// <param name="isLongRunningChild">Set to <see langword="true"/> when the invocations are children of a long-running parent.</param>
        public async Task FireManyAsync(string typeName, IEnumerable<CoreRequestBase> requests, string? correlationId = null, bool isLongRunningChild = false)
        {
            await using var channel = await EnsureExchangeAndCreateChannelAsync();

            foreach (var request in requests)
            {
                var message = new FireAsyncMessage
                {
                    TypeName = typeName,
                    Method = "RunAsync",
                    Request = request,
                    CorrelationId = correlationId,
                    IsLongRunningChild = isLongRunningChild
                };

                var body = Encoding.UTF8.GetBytes(CoreSerializer.Serialize(message));
                await channel.BasicPublishAsync(
                    exchange: options.ExchangeName,
                    routingKey: typeName,
                    mandatory: false,
                    basicProperties: EmptyProps,
                    body: body);
            }
        }

        /// <inheritdoc/>
        public Task ResumeAsync<TService>(string operationId, string? correlationId = null)
            where TService : ICoreLongRunningService
        {
            var type = typeof(TService);
            var typeName = $"{type.FullName}, {type.Assembly.GetName().Name}";
            return ResumeAsync(typeName, operationId, correlationId);
        }

        /// <inheritdoc/>
        public async Task ResumeAsync(string typeName, string operationId, string? correlationId = null)
        {
            var message = new FireAsyncMessage
            {
                TypeName = typeName,
                Method = "ResumeAsync",
                OperationId = operationId,
                CorrelationId = correlationId
            };

            var body = Encoding.UTF8.GetBytes(CoreSerializer.Serialize(message));

            await using var channel = await EnsureExchangeAndCreateChannelAsync();
            await channel.BasicPublishAsync(
                exchange: options.ExchangeName,
                routingKey: typeName,
                mandatory: false,
                basicProperties: EmptyProps,
                body: body);
        }

        private async Task<IChannel> EnsureExchangeAndCreateChannelAsync()
        {
            if (!isInitialized)
            {
                await initLock.WaitAsync();
                try
                {
                    if (!isInitialized)
                    {
                        await using var initChannel = await connection.CreateChannelAsync();
                        await initChannel.ExchangeDeclareAsync(
                            exchange: options.ExchangeName,
                            type: "topic",
                            durable: true,
                            autoDelete: false);
                        isInitialized = true;
                    }
                }
                finally
                {
                    initLock.Release();
                }
            }

            return await connection.CreateChannelAsync();
        }
    }
}
