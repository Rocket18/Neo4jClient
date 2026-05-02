using System;
using System.IO;
using System.Linq;
using System.Text.Json.Serialization;

namespace Neo4jClient.ApiModels
{
    internal class NodeApiResponse<TNode>
    {
        [JsonPropertyName("self")]
        public string Self { get; set; }

        [JsonPropertyName("data")]
        public TNode Data { get; set; }

        public Node<TNode> ToNode(IGraphClient client)
        {
            var nodeId = long.Parse(GetLastPathSegment(Self));
            return new Node<TNode>(Data, new NodeReference<TNode>(nodeId, client));
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
