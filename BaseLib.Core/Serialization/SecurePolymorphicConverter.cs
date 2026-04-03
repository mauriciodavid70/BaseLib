using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using BaseLib.Core.Security;

namespace BaseLib.Core.Serialization
{
    /// <summary>
    /// Support for CoreSecret Attribute 
    /// </summary>
    public class SecurePolymorphicConverter<T> : JsonConverter<T>
        where T : class
    {
        private readonly IEncryptionKeyProvider keyProvider;
        
        /// <summary>Initializes the converter with the key provider used to encrypt/decrypt secret properties.</summary>
        /// <param name="keyProvider">Provider used to generate and unwrap AES-256 data keys.</param>
        public SecurePolymorphicConverter(IEncryptionKeyProvider keyProvider)
        {
            this.keyProvider = keyProvider;
        }

        /// <inheritdoc/>
        public override bool CanConvert(Type typeToConvert)
        {
            return typeof(T).IsAssignableFrom(typeToConvert);
        }

        /// <inheritdoc/>
        public override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            //check start of the object
            if (reader.TokenType != JsonTokenType.StartObject)
            {
                throw new JsonException("Expected StartObject token.");
            }

            //define type
            if (!reader.Read() || reader.GetString() != "___type")
            {
                throw new JsonException("___type expected");
            }
            reader.Read();
            var typeName = reader.GetString() ?? string.Empty;
            var type = Type.GetType(typeName) 
                ??  throw new JsonException($"Type {typeName} could not be found.");;

            //set encryption key if needed
            var isSecret = type.GetProperties().Any(prop => prop.GetCustomAttribute<CoreSecretAttribute>() != null);
            var encryptionKey = Array.Empty<byte>();
            if (isSecret)
            {
                if (!reader.Read() || reader.GetString() != "___key")
                {
                    throw new JsonException("___key expected");
                }
                reader.Read();
                var wrappedKey = Convert.FromBase64String(reader.GetString() ?? string.Empty);
                encryptionKey = this.keyProvider.UnwrapKeyAsync(wrappedKey).GetAwaiter().GetResult();
            }

            // Create an instance of the object type
            var instance = Activator.CreateInstance(type)
                ?? throw new JsonException($"Could not create an instance of {type}.");

            // read the rest of the props and match with the instance props
            while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
            {
                var propertyName = reader.GetString() ?? string.Empty;
                var property = type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);

                if (property != null)
                {
                    //read the value
                    reader.Read();

                    if (isSecret && property.GetCustomAttribute<CoreSecretAttribute>() != null)
                    {
                        // Property is marked as sensitive, decrypt its value
                        var encryptedValue = reader.GetString() ?? string.Empty;
                        var decryptedValue = Decrypt(encryptedValue, encryptionKey);
                        // Deserialize decrypted value and set instance property
                        var propertyValue = JsonSerializer.Deserialize(decryptedValue, property.PropertyType, options);
                        property.SetValue(instance, propertyValue);
                    }
                    else
                    {
                        // Property is not sensitive, deserialize as usual and set instance property
                        var propertyValue = JsonSerializer.Deserialize(ref reader, property.PropertyType, options);
                        property.SetValue(instance, propertyValue);
                    }
                }
                else
                {
                    //ignore, read the value and continue with next prop
                    reader.Read();
                }
            }

            //done
            return instance as T;

        }

        /// <inheritdoc/>
        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
        {
            var type = value.GetType();

            writer.WriteStartObject();

            writer.WriteString("___type", type.AssemblyQualifiedName);

            var key = Array.Empty<byte>();
            var wrappedKey = Array.Empty<byte>();
            var isSecret = type.GetProperties().Any(prop => prop.GetCustomAttribute<CoreSecretAttribute>() != null);
            if( isSecret)
            {
                (key, wrappedKey) = this.keyProvider.GetEncryptionKeyAsync().GetAwaiter().GetResult();
                writer.WriteString("___key", Convert.ToBase64String(wrappedKey));
            }

            // Iterate over the properties of the object and serialize them
            foreach (var property in type.GetProperties())
            {
                var propertyValue = property.GetValue(value);
                writer.WritePropertyName(property.Name);

                if (property.GetCustomAttribute<CoreSecretAttribute>() != null)
                {
                    var serializedValue = JsonSerializer.Serialize(propertyValue);
                    var encryptedValue = Encrypt(serializedValue, key);
                    writer.WriteStringValue(encryptedValue);
                }
                else
                {
                    JsonSerializer.Serialize(writer, propertyValue, property.PropertyType, options);
                }
            }
            writer.WriteEndObject();
        }

        private static string Encrypt(string value, byte[] key)
        {
            using (var algorithm = Aes.Create())
            {
                algorithm.Padding = PaddingMode.PKCS7;
                algorithm.KeySize = 256;
                algorithm.Key = key;
                algorithm.GenerateIV();

                using (var encryptedStream = new MemoryStream())
                {
                    using (var cryptoStream = new CryptoStream(encryptedStream, algorithm.CreateEncryptor(), CryptoStreamMode.Write))
                    using (var writer = new StreamWriter(cryptoStream))
                    {
                        writer.Write(value);
                    }
                    //encrypted data                    
                    var encryptedData = encryptedStream.ToArray();
                    // Prepend the IV to the encrypted data
                    var encryptedDataWithIV = new byte[algorithm.IV.Length + encryptedData.Length];
                    Buffer.BlockCopy(algorithm.IV, 0, encryptedDataWithIV, 0, algorithm.IV.Length);
                    Buffer.BlockCopy(encryptedData, 0, encryptedDataWithIV, algorithm.IV.Length, encryptedData.Length);
                    //convert to base64
                    var encryptedValue = Convert.ToBase64String(encryptedDataWithIV);
                    //done
                    return encryptedValue;
                }

            }
        }

        private static string Decrypt(string encryptedValue, byte[] key)
        {
            var encryptedDataWithIV = Convert.FromBase64String(encryptedValue);
            //The IV byte array:
            var iv = new byte[16];
            Buffer.BlockCopy(encryptedDataWithIV, 0, iv, 0, iv.Length);
            //the encrypted data bytes:
            var encryptedData = new byte[encryptedDataWithIV.Length - iv.Length];
            Buffer.BlockCopy(encryptedDataWithIV, iv.Length, encryptedData, 0, encryptedData.Length);

            using (var algorithm = Aes.Create())
            {
                algorithm.Padding = PaddingMode.PKCS7;
                algorithm.KeySize = 256;
                algorithm.Key = key;
                algorithm.IV = iv;

                using (var encryptedStream = new MemoryStream(encryptedData))
                using (var cryptoStream = new CryptoStream(encryptedStream, algorithm.CreateDecryptor(), CryptoStreamMode.Read))
                using (var reader = new StreamReader(cryptoStream))
                {
                    return reader.ReadToEnd();
                }
            }
        }




    }
}