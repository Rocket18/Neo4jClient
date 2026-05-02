using System.Text.Json.Nodes;

namespace Neo4jClient.Serialization
{
    public class PartialDeserializationContext
    {
        public JsonNode RootResult { get; set; }
        public DeserializationContext DeserializationContext { get; set; }
    }
}
