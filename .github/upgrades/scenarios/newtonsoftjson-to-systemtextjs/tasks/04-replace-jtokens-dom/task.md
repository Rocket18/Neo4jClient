# 04-replace-jtokens-dom: replace-jtokens-dom

Replace the `JToken`/`JArray`/`JValue`/`JObject` DOM usage throughout the deserializer with `System.Text.Json.Nodes` (`JsonNode`/`JsonArray`/`JsonObject`) or `JsonElement`/`JsonDocument`. This is the highest-risk task.

Files:
- `Serialization/CommonDeserializerMethods.cs` — core DOM traversal, date parsing, value coercion
- `Serialization/CypherJsonDeserializer.cs` — `JsonTextReader` with `DateParseHandling.None`, `JToken.ReadFrom`
- `JTokenExtensions.cs` — entire file is a `JToken` extension; replace or fold into new utilities
- `Serialization/PartialDeserializationContext.cs` — public `JToken RootResult` property

Key behavioral concerns:
- `DateParseHandling.None` must be preserved — use `JsonDocumentOptions` / read dates as strings
- `JToken.SelectToken(jsonPath)` has no direct equivalent — rewrite path navigation manually
- `JToken.Value<T>()` — use `JsonNode` indexing or `JsonElement.GetProperty`
- `token.AsString()` extension must be ported

**Done when**: All `JToken`/`JArray`/`JValue` references are removed; deserialization logic compiles and behaves correctly with `System.Text.Json` DOM types.
