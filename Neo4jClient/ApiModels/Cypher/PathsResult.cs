using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Neo4jClient.ApiModels.Cypher
{
    public class PathsResult
    {
        [JsonPropertyName("start")]
        public string Start { get; set; }

        [JsonPropertyName("nodes")]
        public List<string> Nodes { get; set; }

        [JsonPropertyName("length")]
        public int Length { get; set; }

        [JsonPropertyName("relationships")]
        public List<string> Relationships { get; set; }

        [JsonPropertyName("end")]
        public string End { get; set; }
    }
}
