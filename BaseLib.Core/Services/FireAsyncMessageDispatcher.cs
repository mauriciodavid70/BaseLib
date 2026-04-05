using BaseLib.Core.Serialization;

namespace BaseLib.Core.Services
{
    /// <summary>
    /// Transport-agnostic dispatcher for Fire-and-Forget async service invocations.
    /// Deserializes an <see cref="ICoreMessageEnvelope"/> body as a <see cref="FireAsyncMessage"/>
    /// and routes the call to <see cref="ICoreServiceRunner.RunAsync"/> or
    /// <see cref="ICoreServiceRunner.ResumeAsync"/> based on the <see cref="FireAsyncMessage.Method"/> field.
    /// This class is stateless and safe to register as a singleton.
    /// </summary>
    public class FireAsyncMessageDispatcher(ICoreServiceRunner runner)
    {
        /// <summary>
        /// Deserializes the <paramref name="envelope"/> body as a <see cref="FireAsyncMessage"/> and dispatches
        /// the call to <see cref="ICoreServiceRunner.RunAsync"/> or <see cref="ICoreServiceRunner.ResumeAsync"/>.
        /// </summary>
        /// <param name="envelope">The message envelope containing the raw JSON payload.</param>
        /// <exception cref="NotSupportedException">
        /// Thrown when the <see cref="FireAsyncMessage.Method"/> field is not <c>RunAsync</c> or <c>ResumeAsync</c>.
        /// </exception>
        public async Task DispatchAsync(ICoreMessageEnvelope envelope)
        {
            var message = CoreSerializer.Deserialize<FireAsyncMessage>(envelope.Body)
                ?? throw new NullReferenceException("No Service Name on payload");

            var typeName = message.TypeName
                ?? throw new NullReferenceException("No Service Name on payload");

            if (string.IsNullOrEmpty(message.Method) || message.Method.Equals("RunAsync", StringComparison.OrdinalIgnoreCase))
            {
                var request = message.Request ?? throw new NullReferenceException("No Request on payload");
                await runner.RunAsync(typeName, request, message.CorrelationId, message.IsLongRunningChild);
            }
            else if (message.Method.Equals("ResumeAsync", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrEmpty(message.OperationId))
                    throw new NullReferenceException("No OperationId on payload");
                await runner.ResumeAsync(typeName, message.OperationId!);
            }
            else
            {
                throw new NotSupportedException($"Method '{message.Method}' is not supported.");
            }
        }
    }
}
