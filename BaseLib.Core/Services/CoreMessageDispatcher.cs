using BaseLib.Core.Models;
using BaseLib.Core.Serialization;

namespace BaseLib.Core.Services
{
    /// <summary>
    /// Transport-agnostic dispatcher that deserializes an <see cref="IMessageEnvelope"/> body
    /// and routes the call to <see cref="ICoreServiceRunner.RunAsync"/> or
    /// <see cref="ICoreServiceRunner.ResumeAsync"/> based on the <c>Method</c> field in the payload.
    /// This class is stateless and safe to register as a singleton.
    /// </summary>
    public class CoreMessageDispatcher
    {
        private readonly ICoreServiceRunner runner;

        /// <summary>
        /// Initialises the dispatcher with the service runner used to execute dispatched messages.
        /// </summary>
        /// <param name="runner">Runner that resolves and executes services by assembly-qualified type name.</param>
        public CoreMessageDispatcher(ICoreServiceRunner runner)
        {
            this.runner = runner;
        }

        /// <summary>
        /// Deserializes the <paramref name="envelope"/> body and dispatches the call to
        /// <see cref="ICoreServiceRunner.RunAsync"/> or <see cref="ICoreServiceRunner.ResumeAsync"/>.
        /// </summary>
        /// <param name="envelope">The message envelope containing the raw JSON payload.</param>
        /// <exception cref="NotSupportedException">
        /// Thrown when the <c>Method</c> field in the payload is not <c>RunAsync</c> or <c>ResumeAsync</c>.
        /// </exception>
        public async Task DispatchAsync(IMessageEnvelope envelope)
        {
            var payload = CoreSerializer.Deserialize<Payload>(envelope.Body)
                ?? throw new NullReferenceException("No Service Name on payload");

            var typeName = payload.TypeName
                ?? throw new NullReferenceException("No Service Name on payload");

            if (string.IsNullOrEmpty(payload.Method) || payload.Method.Equals("RunAsync", StringComparison.OrdinalIgnoreCase))
            {
                var request = payload.Request ?? throw new NullReferenceException("No Request on payload");
                await runner.RunAsync(typeName, request, payload.CorrelationId, payload.IsLongRunningChild);
            }
            else if (payload.Method.Equals("ResumeAsync", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrEmpty(payload.OperationId))
                    throw new NullReferenceException("No OperationId on payload");
                await runner.ResumeAsync(typeName, payload.OperationId!);
            }
            else
            {
                throw new NotSupportedException($"Method '{payload.Method}' is not supported.");
            }
        }

        private class Payload
        {
            public string? TypeName { get; set; }
            public CoreRequestBase? Request { get; set; }
            public string? OperationId { get; set; }
            public string? CorrelationId { get; set; }
            public bool IsLongRunningChild { get; set; }
            public string? Method { get; set; }
        }
    }
}
