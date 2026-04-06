using System.Text;
using BaseLib.Core.Models;
using BaseLib.Core.Serialization;
using NATS.Client.JetStream;

namespace BaseLib.Core.Services.Nats
{
    /// <summary>
    /// <see cref="ICoreServiceFireOnly"/> implementation that dispatches service invocations
    /// as persistent NATS JetStream messages. Uses <see cref="INatsJSContext.PublishAsync"/>
    /// to guarantee at-least-once delivery, matching the reliability level of the SQS-based
    /// implementation. Register as a singleton.
    /// </summary>
    public class NatsCoreServiceFireOnly : ICoreServiceFireOnly
    {
        private readonly INatsJSContext js;
        private readonly NatsTransportOptions options;

        /// <summary>Initializes the fire-only dispatcher.</summary>
        /// <param name="js">The NATS JetStream context used to publish persistent messages.</param>
        /// <param name="options">Stream name and dispatch subject configuration.</param>
        public NatsCoreServiceFireOnly(INatsJSContext js, NatsTransportOptions options)
        {
            this.js = js;
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

            var payload = Encoding.UTF8.GetBytes(CoreSerializer.Serialize(message));
            await js.PublishAsync<byte[]>(
                subject: options.DispatchSubject,
                data: payload,
                serializer: null,
                opts: null,
                cancellationToken: default);
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
        /// Dispatches multiple service invocations by type name as persistent JetStream messages.
        /// </summary>
        /// <param name="typeName">Assembly-qualified type name of the target service.</param>
        /// <param name="requests">Collection of request payloads.</param>
        /// <param name="correlationId">Correlation ID written into each message envelope.</param>
        /// <param name="isLongRunningChild">Set to <see langword="true"/> when the invocations are children of a long-running parent.</param>
        public async Task FireManyAsync(string typeName, IEnumerable<CoreRequestBase> requests, string? correlationId = null, bool isLongRunningChild = false)
        {
            foreach (var request in requests)
            {
                await FireAsync(typeName, request, correlationId, isLongRunningChild);
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

            var payload = Encoding.UTF8.GetBytes(CoreSerializer.Serialize(message));
            await js.PublishAsync<byte[]>(
                subject: options.DispatchSubject,
                data: payload,
                serializer: null,
                opts: null,
                cancellationToken: default);
        }
    }
}
