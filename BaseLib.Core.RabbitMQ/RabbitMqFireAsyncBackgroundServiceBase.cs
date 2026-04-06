using System.Text;
using BaseLib.Core.Serialization;
using RabbitMQ.Client;

namespace BaseLib.Core.Services.RabbitMQ
{
    /// <summary>
    /// Abstract base class for RabbitMQ-backed Fire-and-Forget async service consumers.
    /// Subclasses provide the durable queue name via <see cref="QueueName"/> and
    /// inherit the receive/ack/nack loop from <see cref="FireAsyncBackgroundServiceBase"/>.
    /// Messages are pulled via <c>BasicGetAsync</c>; acknowledged on success via
    /// <c>BasicAckAsync</c> and nacked without requeue on failure via <c>BasicNackAsync</c>.
    /// </summary>
    public abstract class RabbitMqFireAsyncBackgroundServiceBase : FireAsyncBackgroundServiceBase
    {
        private readonly IConnection connection;
        private IChannel? channel;
        private BasicGetResult? lastResult;

        /// <summary>Initializes the background consumer.</summary>
        /// <param name="dispatcher">The Fire-and-Forget message dispatcher.</param>
        /// <param name="connection">Long-lived RabbitMQ connection (singleton).</param>
        protected RabbitMqFireAsyncBackgroundServiceBase(
            FireAsyncMessageDispatcher dispatcher,
            IConnection connection)
            : base(dispatcher)
        {
            this.connection = connection;
        }

        /// <summary>
        /// The name of the durable RabbitMQ queue this consumer reads from.
        /// Concrete subclasses must provide this value.
        /// </summary>
        protected abstract string QueueName { get; }

        /// <summary>
        /// Pulls the next message from <see cref="QueueName"/> using <c>BasicGetAsync</c>.
        /// Returns <see langword="null"/> if no message is currently available.
        /// </summary>
        /// <param name="cancellationToken">Token that signals cancellation.</param>
        /// <returns>The next message envelope, or <see langword="null"/> if the queue is empty.</returns>
        protected override async Task<ICoreMessageEnvelope?> ReceiveAsync(CancellationToken cancellationToken)
        {
            channel ??= await connection.CreateChannelAsync(cancellationToken: cancellationToken);

            lastResult = await channel.BasicGetAsync(QueueName, autoAck: false, cancellationToken);

            if (lastResult == null)
                return null;

            var body = Encoding.UTF8.GetString(lastResult.Body.Span);
            return new RabbitMqMessageEnvelope(body, lastResult.DeliveryTag.ToString(), lastResult.DeliveryTag);
        }

        /// <summary>
        /// Acknowledges the message by calling <c>BasicAckAsync</c> on its delivery tag.
        /// </summary>
        /// <param name="envelope">The envelope that was successfully dispatched.</param>
        /// <param name="cancellationToken">Token that signals cancellation.</param>
        protected override async Task AcknowledgeAsync(ICoreMessageEnvelope envelope, CancellationToken cancellationToken)
        {
            if (channel != null && envelope is RabbitMqMessageEnvelope rmqEnvelope)
            {
                await channel.BasicAckAsync(rmqEnvelope.DeliveryTag, multiple: false, cancellationToken);
            }
        }

        /// <summary>
        /// Negatively acknowledges the message by calling <c>BasicNackAsync</c> with
        /// <c>requeue = false</c>, routing it to a dead-letter exchange if configured.
        /// </summary>
        /// <param name="envelope">The envelope whose dispatch failed.</param>
        /// <param name="cancellationToken">Token that signals cancellation.</param>
        protected override async Task NackAsync(ICoreMessageEnvelope envelope, CancellationToken cancellationToken)
        {
            if (channel != null && envelope is RabbitMqMessageEnvelope rmqEnvelope)
            {
                await channel.BasicNackAsync(rmqEnvelope.DeliveryTag, multiple: false, requeue: false, cancellationToken);
            }
        }

        /// <summary>
        /// Disposes the managed RabbitMQ channel in addition to the hosted service lifecycle.
        /// </summary>
        public override void Dispose()
        {
            channel?.Dispose();
            channel = null;
            base.Dispose();
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Internal <see cref="ICoreMessageEnvelope"/> implementation that carries the
        /// RabbitMQ delivery tag alongside the JSON body for acknowledgement operations.
        /// </summary>
        private sealed record RabbitMqMessageEnvelope(string Body, string MessageId, ulong DeliveryTag)
            : ICoreMessageEnvelope;
    }
}
