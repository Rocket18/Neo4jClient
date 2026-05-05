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

        private static readonly Neo4jContractResolver _contractResolver = new Neo4jContractResolver();

        private JsonSerializerOptions BuildOptions()
        {
            bool camelCase = JsonSerializerOptions?.PropertyNamingPolicy == JsonNamingPolicy.CamelCase;

            // Build via the contract resolver so [Neo4jIgnore] is always honoured.
            var options = _contractResolver.BuildOptions(
                extraConverters: JsonConverters?.Reverse(),
                camelCase: camelCase,
                ignoreNulls: IgnoreNullValues);

            // Forward any non-default settings from the caller-supplied options.
            if (JsonSerializerOptions != null)
            {
                options.WriteIndented = JsonSerializerOptions.WriteIndented;
                options.Encoder = JsonSerializerOptions.Encoder;
                // Add any converters the caller registered that the contract resolver didn't add.
                foreach (var c in JsonSerializerOptions.Converters)
                    if (!options.Converters.Contains(c))
                        options.Converters.Add(c);
            }
            else
            {
                options.WriteIndented = true;
            }

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
