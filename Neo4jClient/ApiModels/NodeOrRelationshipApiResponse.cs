using System.Text.Json.Serialization;

namespace Neo4jClient.ApiModels
{
    internal class NodeOrRelationshipApiResponse<TNode>
    {
        [JsonPropertyName("data")]
        public TNode Data { get; set; }
    }
}
