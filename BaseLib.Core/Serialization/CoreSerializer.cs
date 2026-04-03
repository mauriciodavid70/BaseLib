namespace BaseLib.Core.Serialization
{
    /// <summary>
    /// Static class provides added core functionality to serialize/deserialize objects or value types to/from JSON
    /// </summary>
    public static class CoreSerializer
    {
        static ICoreSerializer? serializer;

        /// <summary>
        /// Registers the <see cref="ICoreSerializer"/> instance used by all subsequent
        /// <see cref="Serialize{T}"/> and <see cref="Deserialize{T}"/> calls.
        /// Must be called once at application startup before any service runs.
        /// </summary>
        public static void Initialize(ICoreSerializer s)
        {
            serializer = s;
        }

        /// <summary>Serializes <paramref name="value"/> to JSON using the registered serializer.</summary>
        /// <typeparam name="T">The type to serialize.</typeparam>
        public static string Serialize<T>(T value)
        {
            if (serializer == null) throw new NullReferenceException("Serializer not present, invoke static Initialize(ICoreSerializer)");
            return serializer.Serialize(value);
        }

        /// <summary>Deserializes a JSON string to <typeparamref name="T"/> using the registered serializer.</summary>
        /// <typeparam name="T">The target type.</typeparam>
        public static T? Deserialize<T>(string json)
        {
            if (serializer == null) throw new NullReferenceException("Serializer not present, invoke static Initialize(ICoreSerializer)");
            return serializer.Deserialize<T>(json);
        }
    }
}