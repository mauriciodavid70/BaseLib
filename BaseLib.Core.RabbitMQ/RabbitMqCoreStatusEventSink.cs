using System.Text;
using BaseLib.Core.Models;
using BaseLib.Core.Serialization;
using RabbitMQ.Client;

namespace BaseLib.Core.Services.RabbitMQ
{
    /// <summary>
    /// <see cref="ICoreStatusEventSink"/> implementation that publishes domain status events
    /// to a durable RabbitMQ topic exchange. The routing key is <c>{ServiceName}.succeeded</c>
    /// when the response indicates success and <c>{ServiceName}.failed</c> otherwise, allowing
    /// consumers to bind queues with wildcard patterns (e.g. <c>*.failed</c>) to replicate
    /// SNS filter policy behaviour. Register as a singleton.
    /// </summary>
    public class RabbitMqCoreStatusEventSink : ICoreStatusEventSink
    {
        private static readonly BasicProperties EmptyProps = new();

        private readonly IConnection connection;
        private readonly RabbitMqOptions options;
        private bool isInitialized;
        private readonly SemaphoreSlim initLock = new(1, 1);

        /// <summary>Initializes the event sink.</summary>
        /// <param name="connection">Long-lived RabbitMQ connection (singleton).</param>
        /// <param name="options">Exchange name configuration.</param>
        public RabbitMqCoreStatusEventSink(IConnection connection, RabbitMqOptions options)
        {
            this.connection = connection;
            this.options = options;
        }

        /// <inheritdoc/>
        public async Task WriteAsync(CoreStatusEvent statusEvent)
        {
            var succeeded = statusEvent.Response?.Succeeded ?? false;
            var suffix = succeeded ? "succeeded" : "failed";
            var routingKey = $"{statusEvent.ServiceName}.{suffix}";

            var body = Encoding.UTF8.GetBytes(CoreSerializer.Serialize(statusEvent));

            await using var channel = await EnsureExchangeAndCreateChannelAsync();
            await channel.BasicPublishAsync(
                exchange: options.EventExchangeName,
                routingKey: routingKey,
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
                            exchange: options.EventExchangeName,
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
