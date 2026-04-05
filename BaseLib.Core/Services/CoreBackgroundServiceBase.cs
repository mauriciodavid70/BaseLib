using Microsoft.Extensions.Hosting;

namespace BaseLib.Core.Services
{
    /// <summary>
    /// Abstract background service base for Fire-and-Forget async service invocations.
    /// Receives message envelopes from a transport-specific source and delegates execution
    /// to <see cref="CoreMessageDispatcher"/>, which resolves and runs the target
    /// <see cref="ICoreServiceBase"/> implementation via <see cref="ICoreServiceRunner"/>.
    /// Derive from this class to integrate a specific message transport (e.g. NATS, RabbitMQ)
    /// with the BaseLib service dispatch pipeline.
    /// </summary>
    public abstract class CoreBackgroundServiceBase : BackgroundService
    {
        private readonly CoreMessageDispatcher dispatcher;

        /// <summary>
        /// Initialises the background service with the dispatcher used to route messages.
        /// </summary>
        /// <param name="dispatcher">The stateless message dispatcher.</param>
        protected CoreBackgroundServiceBase(CoreMessageDispatcher dispatcher)
        {
            this.dispatcher = dispatcher;
        }

        /// <summary>
        /// Runs the consumer loop: receives envelopes, dispatches them, and
        /// acknowledges or nacks each one until cancellation is requested.
        /// </summary>
        /// <param name="stoppingToken">Token that signals the host is shutting down.</param>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                ICoreMessageEnvelope? envelope = null;
                try
                {
                    envelope = await ReceiveAsync(stoppingToken);
                    if (envelope == null)
                        continue;

                    await dispatcher.DispatchAsync(envelope);
                    await AcknowledgeAsync(envelope, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception)
                {
                    if (envelope != null)
                        await NackAsync(envelope, stoppingToken);
                }
            }
        }

        /// <summary>
        /// Waits for and returns the next message from the transport.
        /// Return <see langword="null"/> if no message is currently available.
        /// </summary>
        /// <param name="cancellationToken">Token that signals cancellation.</param>
        /// <returns>The next message envelope, or <see langword="null"/> if none is available.</returns>
        protected abstract Task<ICoreMessageEnvelope?> ReceiveAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Acknowledges that the given <paramref name="envelope"/> was successfully processed,
        /// causing the transport to remove it from the queue.
        /// </summary>
        /// <param name="envelope">The envelope that was successfully dispatched.</param>
        /// <param name="cancellationToken">Token that signals cancellation.</param>
        protected abstract Task AcknowledgeAsync(ICoreMessageEnvelope envelope, CancellationToken cancellationToken);

        /// <summary>
        /// Negatively acknowledges the given <paramref name="envelope"/>, signalling to the
        /// transport that dispatch failed and the message should be retried or dead-lettered.
        /// </summary>
        /// <param name="envelope">The envelope whose dispatch failed.</param>
        /// <param name="cancellationToken">Token that signals cancellation.</param>
        protected abstract Task NackAsync(ICoreMessageEnvelope envelope, CancellationToken cancellationToken);
    }
}
