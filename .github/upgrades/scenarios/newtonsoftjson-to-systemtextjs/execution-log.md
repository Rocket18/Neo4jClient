
## [2026-05-02 22:32] 01-update-package-reference

Removed `Newtonsoft.Json 13.0.3` and added `System.Text.Json 9.0.5` to Neo4jClient.csproj. Package restore succeeded. Committed.


## [2026-05-02 22:35] 02-swap-model-attributes

Replaced all `[JsonProperty("x")]` attributes with `[JsonPropertyName("x")]` across 11 model/POCO files. Updated `MemberInfoExtensions.cs` to use `JsonPropertyNameAttribute` instead of Newtonsoft's `JsonPropertyAttribute`/`JsonObjectAttribute`/`NamingStrategy`. Build errors remain only in files handled by later tasks. Committed.


## [2026-05-02 22:47] 03-rewrite-custom-converters

Rewrote all 5 custom converters as STJ `JsonConverterFactory`/`JsonConverter<T>` implementations. `EnumValueConverter`, `NullableEnumValueConverter`, and `TypeConverterBasedJsonConverter` use the factory pattern since they handle open-ended types. `TimeZoneInfoConverter` is a straightforward `JsonConverter<TimeZoneInfo>`. `ZonedDateTimeConverter.cs` contained 3 converters (ZonedDateTime, LocalDateTime, DateOnlyConverter) — all rewritten as factories with typed inner converters. No Newtonsoft references remain in these files. Committed.


## [2026-05-02 23:02] 04-replace-jtokens-dom

Replaced all JToken/JArray/JObject/JValue DOM usage with System.Text.Json.Nodes (JsonNode/JsonArray/JsonObject). Rewrote `CommonDeserializerMethods` (traversal, date parsing, value coercion, property mapping), `CypherJsonDeserializer` (reader, transaction handling, projection/single-column modes), `JTokenExtensions` → `JsonNodeExtensions`, `PartialDeserializationContext` (JToken → JsonNode), `DeserializationContext` (IContractResolver → JsonSerializerOptions). Also fixed remaining attribute-only files missed in task 02. Committed.


## [2026-05-02 23:23] 05-rewrite-contract-resolver

Rewrote Neo4jContractResolver as STJ-based options builder (DefaultJsonTypeInfoResolver modifier for Neo4jIgnore + renames). Replaced IContractResolver/DefaultContractResolver throughout public API: IGraphClient, GraphClient, BoltGraphClient, CypherQuery, CypherFluentQuery, QueryWriter, CustomJsonSerializer, CustomJsonDeserializer, HttpContentExtensions, Neo4jDriverExtensions, StatementResultHelper, Transaction environments. All Newtonsoft references removed from main project. Build passes cleanly.


## [2026-05-02 23:23] 06-update-public-api

Completed as part of task 05 — all public API files were updated in the same commit.


## [2026-05-02 23:24] 07-update-serializer-core

Already done as part of task 05 — CustomJsonSerializer was fully rewritten with STJ internals in the same commit.

