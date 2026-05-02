using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Neo4jClient.Serialization;

namespace Neo4jClient
{
    internal static class HttpContentExtensions
    {
        public static Task<string> ReadAsStringAsync(this HttpContent content)
        {
            return content.ReadAsStringAsync();
        }

        public static async Task<T> ReadAsJsonAsync<T>(this HttpContent content, IEnumerable<JsonConverter> jsonConverters, JsonSerializerOptions options)
            where T : new()
        {
            var stringContent = await content.ReadAsStringAsync().ConfigureAwait(false);
            return new CustomJsonDeserializer(jsonConverters, options: options).Deserialize<T>(stringContent);
        }

        public static Task<T> ReadAsJsonAsync<T>(this HttpContent content, IEnumerable<JsonConverter> jsonConverters) where T : new()
        {
            return content.ReadAsJsonAsync<T>(jsonConverters, null);
        }
    }
}
