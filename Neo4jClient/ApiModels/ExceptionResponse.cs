using System.Text.Json.Serialization;

namespace Neo4jClient.ApiModels
{
    internal class ExceptionResponse
    {
        [JsonPropertyName("message")]
        public string Message { get; set; }

        [JsonPropertyName("exception")]
        public string Exception { get; set; }

        [JsonPropertyName("fullname")]
        public string FullName { get; set; }

        [JsonPropertyName("stacktrace")]
        public string[] StackTrace { get; set; }
    }
}
