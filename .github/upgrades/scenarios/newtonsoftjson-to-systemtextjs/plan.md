# Newtonsoft.Json → System.Text.Json Migration Plan

## Overview

**Target**: Replace all Newtonsoft.Json usage in `Neo4jClient` with `System.Text.Json`
**Scope**: 1 library project (primary), 1 test project (secondary) — large serialization surface, ~57 code files, public API breaking changes

## Tasks

### 01-update-package-reference

Remove the `Newtonsoft.Json` package reference from `Neo4jClient.csproj` and add `System.Text.Json`. Since `netstandard2.0` is targeted, an explicit NuGet reference is needed. For `net6.0`, STJ is inbox but an explicit reference ensures version consistency.

**Done when**: `Newtonsoft.Json` package reference is removed from the project file; `System.Text.Json` package reference is present; project restores without errors.

---

### 02-swap-model-attributes

Replace `[JsonProperty("name")]` / `[JsonProperty]` Newtonsoft attributes with `[JsonPropertyName("name")]` (from `System.Text.Json.Serialization`) on all API model classes and other POCO-style files. These files only use Newtonsoft for attributes and have no converter/DOM logic.

Affected files include: all `ApiModels/` files, `Cypher/CypherQuery.cs`, `Cypher/QueryWriter.cs`, `Cypher/CypherFluentQuery.cs`, `Cypher/CypherReturnExpressionBuilder.cs`, `Execution/ExecutionConfiguration.cs`, `Execution/HttpResponseMessageExtensions.cs`, `IndexConfiguration.cs`, `IndexMetaData.cs`, and any others using only `[JsonProperty]`.

**Done when**: All `[JsonProperty]` attributes from Newtonsoft are replaced with `[JsonPropertyName]` or `[JsonIgnore]` equivalents; no `Newtonsoft.Json` imports remain in any of these files; project builds.

---

### 03-rewrite-custom-converters

Rewrite the five custom Newtonsoft `JsonConverter` subclasses as `System.Text.Json.Serialization.JsonConverter<T>` implementations. Each converter must preserve its existing serialization/deserialization behavior exactly.

Files:
- `Serialization/EnumValueConverter.cs`
- `Serialization/NullableEnumValueConverter.cs`
- `Serialization/TimeZoneInfoConverter.cs`
- `Serialization/TypeConverterBasedJsonConverter.cs`
- `Serialization/ZonedDateTimeConverter.cs`

Key differences to handle: `JsonConverter<T>` is generic (separate converters per type or use factory pattern); `Read`/`Write` use `Utf8JsonReader`/`Utf8JsonWriter` instead of `JsonReader`/`JsonWriter`; `CanConvert` is replaced by the generic type parameter or a `JsonConverterFactory`.

**Done when**: All five converters compile using only `System.Text.Json` types; their logic is functionally equivalent to the originals; no Newtonsoft imports remain.

---

### 04-replace-jtokens-dom

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

---

### 05-rewrite-contract-resolver

Replace `Neo4jContractResolver` (which extends `DefaultContractResolver`) with a System.Text.Json equivalent. It currently handles:
1. `[Neo4jIgnore]` attribute — skip property during serialization
2. Runtime property renaming via `renames` dictionary

For STJ, implement this as a custom `JsonSerializerOptions`-aware approach: use `[JsonIgnore]` support in a custom `JsonConverter` or a `DefaultJsonTypeInfoResolver` modifier (available in STJ 7+ / .NET 7+; for older targets, a custom resolver is needed). Since targets include `netstandard2.0`, a compatible approach must work across all TFMs.

Files:
- `Serialization/Neo4jContractSerializer.cs`
- `Serialization/DeserializationContext.cs` — replace `IContractResolver` with `JsonSerializerOptions`

**Done when**: `[Neo4jIgnore]` suppresses serialization; runtime property renaming works; no Newtonsoft types remain; compiles on all target frameworks.

---

### 06-update-public-api

Update the public-facing serialization API surface: `IGraphClient`, `GraphClient`, `BoltGraphClient`, `CustomJsonSerializer`, `Execution/ExecutionConfiguration.cs`. Replace Newtonsoft types (`JsonConverter`, `DefaultContractResolver`) with their STJ equivalents (`JsonConverter` from STJ, `JsonSerializerOptions`).

Also update:
- `Transactions/ITransactionExecutionEnvironment.cs`
- `Transactions/TransactionExecutionEnvironment.cs`
- `HttpContentExtensions.cs`
- `StatementResultHelper.cs`
- `Neo4jDriverExtensions.cs`

**Done when**: No Newtonsoft types appear in any public interface or class signatures; all plumbing compiles; `IGraphClient` exposes STJ types only.

---

### 07-update-serializer-core

Update `CustomJsonSerializer` internals to use `JsonSerializer` from `System.Text.Json` for actual serialization/deserialization. Wire up `JsonSerializerOptions` (converters, naming policy, null handling) to replace the old `JsonSerializer` + `JsonTextWriter` + `IContractResolver` pipeline.

Configure options to preserve existing behavior:
- `PropertyNameCaseInsensitive = true`
- `DefaultIgnoreCondition = WhenWritingNull` (mirrors `NullValueHandling.Ignore`)
- Register all custom converters from task 03

**Done when**: `CustomJsonSerializer.Serialize` and `Deserialize<T>` work correctly using STJ; options are wired to existing converter list and renamed properties.

---

### 08-update-tests

Update test files in `Neo4jClient.Tests` that reference Newtonsoft types directly. Most tests use Newtonsoft only because they configure `JsonConverters` on the client — these will follow the public API changes from task 06.

Files include all 10 test files identified in assessment with direct Newtonsoft references.

**Done when**: All tests compile; test suite passes (or known pre-existing failures are documented).

---

### 09-final-validation

Run a full solution build and test suite. Search for any remaining `Newtonsoft` references. Generate the migration report.

**Done when**: Solution builds clean with zero errors; no `Newtonsoft.Json` references remain in any file; migration report generated at `.github/NewtonsoftJsonToSystemTextJsonReport.md`.
