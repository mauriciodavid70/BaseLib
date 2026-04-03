

using BaseLib.Core.Models;
using BaseLib.Core.Serialization;

namespace BaseLib.Core.Services
{
    /// <summary>Extension methods for <see cref="ICoreServiceRunner"/>.</summary>
    public static class CoreServicesExtensions
    {
        /// <summary>
        /// Deserializes a JSON message body and dispatches it to the appropriate <c>RunAsync</c> or
        /// <c>ResumeAsync</c> method on the runner.
        /// </summary>
        /// <param name="runner">The runner that resolves and executes services.</param>
        /// <param name="messageBody">JSON-encoded payload produced by <see cref="ICoreServiceFireOnly"/>.</param>
        [Obsolete("Derive processor from  CoreServiceMessageProcessorBase instead")]
        public static async Task RunAsync(this ICoreServiceRunner runner, string messageBody)
        {
            var payload = CoreSerializer.Deserialize<Payload>(messageBody)
                ?? throw new NullReferenceException("No Service Name on payload");

            var typeName = payload.TypeName
                ?? throw new NullReferenceException("No Service Name on payload");

            if (string.IsNullOrEmpty(payload.Method) || payload.Method.Equals("RunAsync", StringComparison.OrdinalIgnoreCase))
            {
                var request = payload.Request ?? throw new NullReferenceException("No Request on payload");
                await runner.RunAsync(payload.TypeName, payload.Request, payload.CorrelationId, payload.IsLongRunningChild);
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
