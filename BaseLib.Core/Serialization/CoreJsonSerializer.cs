using System.Text.Json;
using BaseLib.Core.Models;

namespace BaseLib.Core.Serialization
{
    /// <summary>
    /// <see cref="ICoreSerializer"/> implementation backed by <c>System.Text.Json</c>.
    /// Pre-registers <see cref="PolymorphicConverter{T}"/> for <see cref="CoreRequestBase"/>
    /// and <see cref="CoreResponseBase"/> so polymorphic payloads round-trip correctly.
    /// </summary>
    public class CoreJsonSerializer : ICoreSerializer
    {
        private readonly JsonSerializerOptions options;

        /// <summary>Initializes a new instance with polymorphic converters pre-registered.</summary>
        public CoreJsonSerializer()
        {
            this.options = new JsonSerializerOptions();
            this.options.Converters.Add(new PolymorphicConverter<CoreRequestBase>());
            this.options.Converters.Add(new PolymorphicConverter<CoreResponseBase>());
        }

        /// <inheritdoc/>
        public string Serialize<T>(T value)
        {
            return JsonSerializer.Serialize(value, options);
        }

        /// <inheritdoc/>
        public T? Deserialize<T>(string json)
        {
            return JsonSerializer.Deserialize<T>(json, options);
        }
    }
}