using System.Diagnostics;
using System.Net.Http;
using System.Text.Json.Serialization;

namespace Neo4jClient.ApiModels
{
    [DebuggerDisplay("{Id}: {Method} {To}")]
    class BatchStep
    {
        [JsonIgnore]
        public HttpMethod Method { get; set; }

        [JsonPropertyName("method")]
        public string MethodAsString
        {
            get { return Method.Method; }
        }

        [JsonPropertyName("to")]
        public string To { get; set; }

        [JsonPropertyName("body")]
        public object Body { get; set; }

        [JsonPropertyName("id")]
        public int Id { get; set; }
    }
}
