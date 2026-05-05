using System.Text.Json.Nodes;

namespace Neo4jClient
{
    internal static class JsonNodeExtensions
    {
        /// <summary>
        /// Returns the string value of a JSON token.
        /// For string tokens, returns the actual string value (without JSON quotes).
        /// For other value types (numbers, booleans), returns the raw JSON representation.
        /// </summary>
        internal static string AsString(this JsonNode token)
        {
            if (token == null) return null;
            if (token is JsonValue jv && jv.TryGetValue<string>(out var str))
                return str;
            return token.ToJsonString();
        }
    }
}
