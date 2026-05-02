using System.Collections.Generic;
using Neo4jClient.Cypher;
using System.Text.Json.Serialization;

namespace Neo4jClient.ApiModels.Cypher
{
    /// <summary>
    /// Very similar to CypherApiQuery but it's used for opened transactions as their serialization
    /// is different
    /// </summary>
    internal class CypherTransactionStatement
    {
        private readonly string[] formatContents; 

        public CypherTransactionStatement(CypherQuery query)
        {
            Statement = query.QueryText;
            Parameters = query.QueryParameters ?? new Dictionary<string, object>();
            formatContents = new string[] {};
            if (query.IncludeQueryStats)
                IncludeStats = query.IncludeQueryStats;
        }

        [JsonPropertyName("statement")]
        public string Statement { get; }

        [JsonPropertyName("resultDataContents")]
        public IEnumerable<string> ResultDataContents => formatContents;

        [JsonPropertyName("parameters")]
        public IDictionary<string, object> Parameters { get; }

        [JsonPropertyName("includeStats")]
        public bool? IncludeStats { get; }
    }
}
