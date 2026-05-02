using System.Text.Json.Serialization;

namespace Neo4jClient.ApiModels
{
    class RelationshipTemplate
    {
        [JsonPropertyName("to")]
        public string To { get; set; }

        [JsonPropertyName("data")]
        public object Data { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }
    }
}
