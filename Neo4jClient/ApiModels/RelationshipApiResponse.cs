using System;
using System.IO;
using System.Linq;
using System.Text.Json.Serialization;

namespace Neo4jClient.ApiModels
{
    internal class RelationshipApiResponse<TData>
        where TData : class, new()
    {
        [JsonPropertyName("self")]
        public string Self { get; set; }

        [JsonPropertyName("start")]
        public string Start { get; set; }

        [JsonPropertyName("end")]
        public string End { get; set; }

        [JsonPropertyName("type")]
        public string TypeKey { get; set; }

        [JsonPropertyName("data")]
        public TData Data { get; set; }

        public RelationshipInstance<TData> ToRelationshipInstance(IGraphClient client)
        {
            var relationshipId = long.Parse(GetLastPathSegment(Self));
            var startNodeId = long.Parse(GetLastPathSegment(Start));
            var endNodeId = long.Parse(GetLastPathSegment(End));

            return new RelationshipInstance<TData>(
                new RelationshipReference<TData>(relationshipId, client),
                new NodeReference(startNodeId, client),
                new NodeReference(endNodeId, client),
                TypeKey,
                Data);
        }

        public RelationshipReference ToRelationshipReference(IGraphClient client)
        {
            var relationshipId = int.Parse(GetLastPathSegment(Self));
            return new RelationshipReference(relationshipId, client);
        }

        static string GetLastPathSegment(string uri)
        {
            var path = new Uri(uri).AbsolutePath;
            return path
                .Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                .LastOrDefault();
        }
    }
}
