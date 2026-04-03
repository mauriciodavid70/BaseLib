using System.Text.Json;
using BaseLib.Core.Models;
using BaseLib.Core.Security;

namespace BaseLib.Core.Serialization
{
    /// <summary>
    /// <see cref="ICoreSerializer"/> implementation that encrypts properties annotated with
    /// <see cref="CoreSecretAttribute"/> using AES-256 envelope encryption.
    /// Pre-registers <see cref="SecurePolymorphicConverter{T}"/> for <see cref="CoreRequestBase"/>
    /// and <see cref="CoreResponseBase"/>.
    /// </summary>
    public class CoreSecureJsonSerializer : ICoreSerializer
    {
        private readonly JsonSerializerOptions options;

        /// <summary>Initializes the serializer with an encryption key provider.</summary>
        /// <param name="keyProvider">Provider used to generate and unwrap AES-256 data keys.</param>
        public CoreSecureJsonSerializer(IEncryptionKeyProvider keyProvider)
        {
            this.options = new JsonSerializerOptions();
            this.options.Converters.Add(new SecurePolymorphicConverter<CoreRequestBase>(keyProvider));
            this.options.Converters.Add(new SecurePolymorphicConverter<CoreResponseBase>(keyProvider));
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