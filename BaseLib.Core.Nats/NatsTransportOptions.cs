namespace BaseLib.Core.Services.Nats
{
    /// <summary>
    /// Configuration options for the NATS JetStream transport integration.
    /// Controls the stream name and dispatch subject used for service invocation messages.
    /// </summary>
    public class NatsTransportOptions
    {
        /// <summary>
        /// The JetStream stream name that receives service dispatch messages.
        /// </summary>
        public string StreamName { get; set; } = string.Empty;

        /// <summary>
        /// The NATS subject to which <see cref="Services.FireAsyncMessage"/> payloads are published.
        /// </summary>
        public string DispatchSubject { get; set; } = string.Empty;
    }
}
