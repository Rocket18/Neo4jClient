using System.Text.Json.Serialization;

namespace Neo4jClient.ApiModels.Cypher
{
    public class QueryStats
    {
        [JsonPropertyName("contains_updates")] public bool ContainsUpdates { get; set; }
        [JsonPropertyName("nodes_created")] public int NodesCreated { get; set; }
        [JsonPropertyName("nodes_deleted")] public int NodesDeleted { get; set; }
        [JsonPropertyName("properties_set")] public int PropertiesSet { get; set; }
        [JsonPropertyName("relationships_created")] public int RelationshipsCreated { get; set; }
        [JsonPropertyName("relationship_deleted")] public int RelationshipsDeleted { get; set; }
        [JsonPropertyName("labels_added")] public int LabelsAdded { get; set; }
        [JsonPropertyName("labels_removed")] public int LabelsRemoved { get; set; }
        [JsonPropertyName("indexes_added")] public int IndexesAdded { get; set; }
        [JsonPropertyName("indexes_removed")] public int IndexesRemoved { get; set; }
        [JsonPropertyName("constraints_added")] public int ConstraintsAdded { get; set; }
        [JsonPropertyName("constraints_removed")] public int ConstraintsRemoved { get; set; }
        [JsonPropertyName("contains_system_updates")] public bool ContainsSystemUpdates { get; set; }
        [JsonPropertyName("system_updates")] public int SystemUpdates { get; set; }

        public QueryStats(){}
        public QueryStats(Neo4j.Driver.ICounters counters)
        {
            ContainsUpdates = counters.ContainsUpdates;
            NodesCreated = counters.NodesCreated;
            NodesDeleted = counters.NodesDeleted;
            PropertiesSet = counters.PropertiesSet;
            RelationshipsCreated = counters.RelationshipsCreated;
            RelationshipsDeleted = counters.RelationshipsDeleted;
            LabelsAdded = counters.LabelsAdded;
            LabelsRemoved = counters.LabelsRemoved;
            IndexesAdded = counters.IndexesAdded;
            IndexesRemoved = counters.IndexesRemoved;
            ConstraintsAdded = counters.ConstraintsAdded;
            ConstraintsRemoved = counters.ConstraintsRemoved;
            ContainsSystemUpdates = counters.ContainsSystemUpdates;
            SystemUpdates = counters.SystemUpdates;
        }
    }
}