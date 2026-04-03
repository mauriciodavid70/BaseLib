namespace BaseLib.Core.Serialization
{
    /// <summary>
    /// Abstracts JSON serialization so the transport layer (SQS, SNS, etc.) is decoupled
    /// from a specific serializer implementation.
    /// The default implementation is <see cref="CoreJsonSerializer"/>.
    /// Use <c>CoreSecureJsonSerializer</c> when payloads contain fields annotated with
    /// <c>[CoreSecret]</c> that must be encrypted at rest.
    /// </summary>
    public interface ICoreSerializer
    {
        /// <summary>Serializes <paramref name="value"/> to a JSON string.</summary>
        /// <typeparam name="T">The type to serialize.</typeparam>
        /// <param name="value">The object to serialize.</param>
        string Serialize<T>(T value);

        /// <summary>Deserializes a JSON string to an instance of <typeparamref name="T"/>.</summary>
        /// <typeparam name="T">The target type.</typeparam>
        /// <param name="json">The JSON string to deserialize.</param>
        /// <returns>The deserialized object, or <see langword="null"/> if the JSON is null or empty.</returns>
        T? Deserialize<T>(string json);
    }
}