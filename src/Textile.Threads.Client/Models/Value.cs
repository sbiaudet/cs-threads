using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Textile.Threads.Client.Models
{
    public class QueryValue
    {
        [JsonPropertyName("string")]
        public string StringValue { get; set; }

        [JsonPropertyName("bool")]
        public bool BoolValue { get; set; }

        [JsonPropertyName("float")]
        public float FloatValue { get; set; }

        public static QueryValue FromObject(object obj)
        {
            if (obj is string @string)
            {
                return new QueryValue()
                {
                    StringValue = @string
                };
            }

            if (obj is bool boolean)
            {
                return new QueryValue()
                {
                    BoolValue = boolean
                };
            }

#pragma warning disable IDE0046 // Convert to conditional expression
            if (obj is float single)
#pragma warning restore IDE0046 // Convert to conditional expression
            {
                return new QueryValue()
                {
                    FloatValue = single
                };
            }

            return new QueryValue();
        }
    }
}
