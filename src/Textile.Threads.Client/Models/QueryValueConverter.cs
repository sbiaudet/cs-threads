using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Textile.Threads.Client.Models
{
    public class QueryValueConverter : JsonConverter<object>
    {
        public override object Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }

        public override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();

            switch (value)
            {
                case string:
                    writer.WritePropertyName("string");
                    writer.WriteStringValue((string)value);
                    break;
                case bool:
                    writer.WritePropertyName("bool");
                    writer.WriteBooleanValue((bool)value);
                    break;
                case float:
                    writer.WritePropertyName("float");
                    writer.WriteNumberValue((float)value);
                    break;
                default:
                    break;
            }

            writer.WriteEndObject();
        }
    }
}