namespace BaseLib.Core.Services
{
    /// <summary>
    /// Transport-agnostic wrapper around a single broker message.
    /// Provides the raw JSON payload body and a unique message identifier
    /// without exposing any transport-specific metadata to the dispatch layer.
    /// </summary>
    public interface IMessageEnvelope
    {
        /// <summary>
        /// The raw JSON payload string to be deserialized and dispatched.
        /// </summary>
        string Body { get; }

        /// <summary>
        /// A unique identifier for this message, used for acknowledgement and failure tracking.
        /// </summary>
        string MessageId { get; }
    }
}
