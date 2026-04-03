using System.Text.Json;
using System.Text.Json.Serialization;
using BaseLib.Core.Models;

namespace BaseLib.Core.Serialization
{
    /// <summary>
    /// Only to Flatten the response to support older consumers
    /// </summary>
    public class FlattenCoreReasonCodeConverter : JsonConverter<CoreResponseBase>
    {
        /// <inheritdoc/>
        public override bool CanConvert(Type typeToConvert)
        {
            var canConvert = typeof(CoreResponseBase).IsAssignableFrom(typeToConvert);
            return canConvert;
        }

        /// <inheritdoc/>
        public override CoreResponseBase? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public override void Write(Utf8JsonWriter writer, CoreResponseBase value, JsonSerializerOptions options)
        {
            var type = value.GetType();
            writer.WriteStartObject();


            // loop through properties
            foreach (var prop in type.GetProperties())
            {
                var propValue = prop.GetValue(value);
                if (propValue is CoreReasonCode reasonCode)
                {
                    // Flatten CoreReasonCode properties
                    writer.WriteNumber(prop.Name, reasonCode.Value);
                    var reasonName = options.PropertyNamingPolicy == null ? "Reason" : "reason";
                    writer.WriteString(reasonName, reasonCode.Description);
                }
                else
                {
                    writer.WritePropertyName(prop.Name);
                    JsonSerializer.Serialize(writer, propValue, options);
                }
            }
            writer.WriteEndObject();
        }
    }
}