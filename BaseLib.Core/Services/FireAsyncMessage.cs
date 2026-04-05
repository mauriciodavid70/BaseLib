using BaseLib.Core.Models;

namespace BaseLib.Core.Services
{
    /// <summary>
    /// Typed message envelope for Fire-and-Forget async service invocations.
    /// Serialized to JSON and carried by <see cref="ICoreMessageEnvelope.Body"/> across any transport.
    /// Deserialized by <see cref="FireAsyncMessageDispatcher"/> to route the call to
    /// <see cref="ICoreServiceRunner.RunAsync"/> or <see cref="ICoreServiceRunner.ResumeAsync"/>.
    /// </summary>
    public class FireAsyncMessage
    {
        /// <summary>Assembly-qualified type name of the target service (e.g. <c>"MyApp.MyService, MyApp"</c>).</summary>
        public string? TypeName { get; set; }

        /// <summary>
        /// Dispatch method: <c>"RunAsync"</c> (default when absent) or <c>"ResumeAsync"</c>.
        /// </summary>
        public string? Method { get; set; }

        /// <summary>The request payload; required when <see cref="Method"/> is <c>"RunAsync"</c>.</summary>
        public CoreRequestBase? Request { get; set; }

        /// <summary>The operation identifier; required when <see cref="Method"/> is <c>"ResumeAsync"</c>.</summary>
        public string? OperationId { get; set; }

        /// <summary>Optional correlation identifier to link related operations across services.</summary>
        public string? CorrelationId { get; set; }

        /// <summary><see langword="true"/> when this invocation is a child spawned by a long-running parent service.</summary>
        public bool IsLongRunningChild { get; set; }
    }
}
