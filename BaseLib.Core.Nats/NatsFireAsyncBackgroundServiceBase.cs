using System.Text;
using NATS.Client.JetStream;

namespace BaseLib.Core.Services.Nats
{
    /// <summary>
    /// Abstract base class for NATS JetStream-backed Fire-and-Forget async service consumers.
    /// Uses a pull consumer (<c>NextAsync</c>) to receive messages from a durable JetStream consumer.
    /// Subclasses may override <see cref="PollTimeout"/> to tune the fetch interval.
    /// When the queue is empty, a 100 ms delay is applied before the next poll to avoid busy-waiting.
    /// Messages are acknowledged on successful dispatch and nacked on failure.
    /// </summary>
    public abstract class NatsFireAsyncBackgroundServiceBase : FireAsyncBackgroundServiceBase
    {
        private readonly INatsJSConsumer consumer;

        /// <summary>Initializes the background consumer.</summary>
        /// <param name="dispatcher">The Fire-and-Forget message dispatcher.</param>
        /// <param name="consumer">The durable JetStream pull consumer to read from.</param>
        protected NatsFireAsyncBackgroundServiceBase(
            FireAsyncMessageDispatcher dispatcher,
            INatsJSConsumer consumer)
            : base(dispatcher)
        {
            this.consumer = consumer;
        }

        /// <summary>
        /// The maximum time to wait for a message before returning <see langword="null"/>.
        /// Defaults to 2 seconds. Override in subclasses to adjust the poll interval.
        /// </summary>
        protected virtual TimeSpan PollTimeout => TimeSpan.FromSeconds(2);

        /// <summary>
        /// Pulls the next message from the JetStream consumer using <c>NextAsync</c>.
        /// Returns <see langword="null"/> on timeout (empty queue), then applies a 100 ms
        /// delay before the caller polls again to prevent busy-waiting.
        /// </summary>
        /// <param name="cancellationToken">Token that signals cancellation.</param>
        /// <returns>The next message envelope, or <see langword="null"/> if no message is available.</returns>
        protected override async Task<ICoreMessageEnvelope?> ReceiveAsync(CancellationToken cancellationToken)
        {
            var opts = new NatsJSNextOpts { Expires = PollTimeout };

            INatsJSMsg<byte[]>? msg;
            try
            {
                msg = await consumer.NextAsync<byte[]>(opts: opts, cancellationToken: cancellationToken);
            }
            catch (NatsJSTimeoutException)
            {
                // Empty queue — apply backoff delay to avoid busy-waiting
                await Task.Delay(100, cancellationToken);
                return null;
            }

            if (msg == null)
            {
                await Task.Delay(100, cancellationToken);
                return null;
            }

            var body = Encoding.UTF8.GetString(msg.Data ?? Array.Empty<byte>());
            return new NatsMessageEnvelope(body, Guid.NewGuid().ToString(), msg);
        }

        /// <summary>
        /// Acknowledges the message by calling <c>AckAsync</c> on the underlying NATS message.
        /// </summary>
        /// <param name="envelope">The envelope that was successfully dispatched.</param>
        /// <param name="cancellationToken">Token that signals cancellation.</param>
        protected override async Task AcknowledgeAsync(ICoreMessageEnvelope envelope, CancellationToken cancellationToken)
        {
            if (envelope is NatsMessageEnvelope natsEnvelope)
            {
                await natsEnvelope.Message.AckAsync(cancellationToken: cancellationToken);
            }
        }

        /// <summary>
        /// Negatively acknowledges the message by calling <c>NakAsync</c> on the underlying NATS message,
        /// signalling to the server that the message should be redelivered or dead-lettered.
        /// </summary>
        /// <param name="envelope">The envelope whose dispatch failed.</param>
        /// <param name="cancellationToken">Token that signals cancellation.</param>
        protected override async Task NackAsync(ICoreMessageEnvelope envelope, CancellationToken cancellationToken)
        {
            if (envelope is NatsMessageEnvelope natsEnvelope)
            {
                await natsEnvelope.Message.NakAsync(cancellationToken: cancellationToken);
            }
        }

        /// <summary>
        /// Internal <see cref="ICoreMessageEnvelope"/> implementation that wraps a
        /// <see cref="INatsJSMsg{T}"/> for acknowledgement operations.
        /// </summary>
        private sealed record NatsMessageEnvelope(
            string Body,
            string MessageId,
            INatsJSMsg<byte[]> Message)
            : ICoreMessageEnvelope;
    }
}
