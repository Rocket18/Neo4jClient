using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Neo4jClient.Serialization
{
    public class CustomJsonSerializer : ISerializer
    {
        public IEnumerable<JsonConverter> JsonConverters { get; set; }
        public string ContentType { get; set; }
        public string DateFormat { get; set; }
        public string Namespace { get; set; }
        public string RootElement { get; set; }
        public bool IgnoreNullValues { get; set; }
        public JsonSerializerOptions JsonSerializerOptions { get; set; }

        public CustomJsonSerializer()
        {
            ContentType = "application/json";
            IgnoreNullValues = true;
        }

        private JsonSerializerOptions BuildOptions()
        {
            var options = JsonSerializerOptions != null
                ? new JsonSerializerOptions(JsonSerializerOptions)
                : new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    DefaultIgnoreCondition = IgnoreNullValues
                        ? JsonIgnoreCondition.WhenWritingNull
                        : JsonIgnoreCondition.Never,
                };

            if (JsonConverters != null)
                foreach (var c in JsonConverters.Reverse())
                    options.Converters.Add(c);

            return options;
        }

        public string Serialize(object obj)
        {
            return JsonSerializer.Serialize(obj, obj?.GetType() ?? typeof(object), BuildOptions());
        }

        public T Deserialize<T>(string content)
        {
            return JsonSerializer.Deserialize<T>(content, BuildOptions());
        }
    }
}
