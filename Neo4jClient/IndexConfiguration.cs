using System.Text.Json.Serialization;

namespace Neo4jClient
{
    public class IndexConfiguration
    {
        [JsonPropertyName("type")]
        public IndexType Type { get; set; }
        [JsonPropertyName("provider")]
        public IndexProvider Provider { get; set; }
    }
}