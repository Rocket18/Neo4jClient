using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Neo4jClient.Serialization
{
    public class DeserializationContext
    {
        public CultureInfo Culture { get; set; }
        public JsonConverter[] JsonConverters { get; set; }
        public JsonSerializerOptions JsonSerializerOptions { get; set; }
    }
}
