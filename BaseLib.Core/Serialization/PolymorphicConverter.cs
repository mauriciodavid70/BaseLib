using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BaseLib.Core.Serialization
{
    /// <summary>
    /// Intended to support polymorphic serialization on specific cases
    /// Only when serialization/deserialization is happening within the context of 
    /// the Core inner calls.
    /// </summary>
    public class PolymorphicConverter<T> : JsonConverter<T>
        where T : class
    {
        
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
                    var propertyValue = JsonSerializer.Deserialize(ref reader, property.PropertyType, options);
                    property.SetValue(instance, propertyValue);
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

            // Iterate over the properties of the object and serialize them
            foreach (var property in type.GetProperties())
            {
                var propertyValue = property.GetValue(value);
                writer.WritePropertyName(property.Name);
                JsonSerializer.Serialize(writer, propertyValue, property.PropertyType, options);
            }
            
            writer.WriteEndObject();
        }
    }
}