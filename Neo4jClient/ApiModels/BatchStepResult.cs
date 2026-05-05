using System.Net;
using System.Text.Json.Serialization;

namespace Neo4jClient.ApiModels
{
    class BatchStepResult
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("from")]
        public string From { get; set; }

        [JsonPropertyName("location")]
        public string Location { get; set; }

        [JsonPropertyName("status")]
        public HttpStatusCode Status { get; set; }

        [JsonPropertyName("body")]
        public string Body { get; set; }
    }
}
