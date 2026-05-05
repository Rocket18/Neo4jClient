using System.Text.Json.Serialization;

namespace Neo4jClient
{
    public class IndexMetaData
    {
        [JsonPropertyName("to_lower_case")]
        public bool ToLowerCase { get; set; }

        [JsonPropertyName("template")]
        public string Template { get; set; }

        [JsonPropertyName("provider")]
        public string Provider { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }
    }
}