# Assessment: Newtonsoft.Json → System.Text.Json Migration

## Executive Summary

**Verdict: HIGH COMPLEXITY — Full drop-in replacement is NOT possible.**

`Newtonsoft.Json` types are part of the **public API** of the `Neo4jClient` library. `JsonConverter`, `DefaultContractResolver`, `JToken`, `JArray`, and related types are exposed in public interfaces (`IGraphClient`) and public classes. This means migrating to `System.Text.Json` is a **breaking change for downstream consumers** of the library, not just an internal implementation detail.

---

## Projects Affected

| Project | Has Direct Reference | Newtonsoft Files |
|---|---|---|
| `Neo4jClient/Neo4jClient.csproj` | ✅ Yes | 30+ files |
| `Neo4jClient.Tests/` | ❌ Transitive only | 10 test files |
| `Neo4jClient.FSharp.Tests/` | ❌ Transitive only | 0 |
| `Neo4jClient.Vb.Tests/` | ❌ Transitive only | 0 |

---

## Usage Categories

### 🔴 PUBLIC API SURFACE (Breaking Changes)
These are exposed in public interfaces/classes and will break downstream consumers:

| Location | Newtonsoft Type | Notes |
|---|---|---|
| `IGraphClient` | `List<JsonConverter>` | Public property on the main client interface |
| `IGraphClient` | `DefaultContractResolver` | Public property on the main client interface |
| `GraphClient` | `JsonConverter[]`, `DefaultContractResolver` | Public API implementation |
| `BoltGraphClient` | Same as above | Public API implementation |
| `CustomJsonSerializer` | `JsonConverter`, `NullValueHandling`, `IContractResolver` | Public serializer class |
| `DeserializationContext` | `JsonConverter[]`, `IContractResolver` | Public context class |
| `PartialDeserializationContext` | `JToken` | Public context class |

### 🟡 CUSTOM CONVERTERS (Require Rewriting)
These extend Newtonsoft's `JsonConverter` abstract class and must be rewritten using `System.Text.Json.Serialization.JsonConverter<T>`:

| File | Converter |
|---|---|
| `EnumValueConverter.cs` | `JsonConverter` → `JsonConverter<T>` |
| `NullableEnumValueConverter.cs` | `JsonConverter` → `JsonConverter<T>` |
| `TimeZoneInfoConverter.cs` | `JsonConverter` → `JsonConverter<T>` |
| `TypeConverterBasedJsonConverter.cs` | `JsonConverter` → `JsonConverter<T>` |
| `ZonedDateTimeConverter.cs` | `JsonConverter` → `JsonConverter<T>` |

### 🟡 CONTRACT RESOLVER (Requires Rewriting)
`Neo4jContractResolver` extends `DefaultContractResolver` to support `[Neo4jIgnore]` and runtime property renaming. System.Text.Json uses `JsonNamingPolicy` and `JsonIgnoreAttribute` instead — the runtime rename feature requires a custom `JsonConverter` or source generator.

| File | Pattern |
|---|---|
| `Neo4jContractSerializer.cs` | `DefaultContractResolver` → Custom `JsonSerializerOptions` + `JsonConverter` |

### 🔴 JTOKEN/JARRAY DOM (Requires Full Rewrite)
The deserializer uses `JToken`, `JArray`, `JValue`, and `JToken.SelectToken` extensively. System.Text.Json's DOM equivalent is `JsonElement`/`JsonDocument` but has significant API differences:

| File | Newtonsoft DOM Usage |
|---|---|
| `CommonDeserializerMethods.cs` | `JToken`, `JArray`, `JValue`, `JToken.Parse`, `SelectToken`, `Value<T>()` |
| `CypherJsonDeserializer.cs` | `JToken.ReadFrom`, `JsonTextReader`, `DateParseHandling` |
| `JTokenExtensions.cs` | `JToken`, `JTokenType` — entire file |
| `PartialDeserializationContext.cs` | `JToken` — public property |

### 🟡 JSONSERIALIZER USAGE
| File | Usage |
|---|---|
| `CustomJsonSerializer.cs` | `JsonSerializer`, `JsonTextWriter`, `Formatting`, `MissingMemberHandling` |
| `Neo4jContractSerializer.cs` | `JsonProperty`, `MemberSerialization` |
| `StatementResultHelper.cs` | `JsonConvert.SerializeObject` / `DeserializeObject` |
| `Neo4jDriverExtensions.cs` | `JsonConvert` usage |
| `HttpContentExtensions.cs` | `JsonConvert` usage |
| `BoltGraphClient.cs` | Serializer plumbing |
| `GraphClient.cs` | Serializer plumbing, `DefaultJsonConverters`, `DefaultJsonContractResolver` |

### 🟢 ATTRIBUTE-ONLY (Simple Change)
Files only using `[JsonProperty]` attribute on model classes:

| File | Change Needed |
|---|---|
| `ApiModels/` (8 files) | `[JsonProperty("name")]` → `[JsonPropertyName("name")]` |
| `Cypher/CypherQuery.cs` | Same |
| `Cypher/QueryWriter.cs` | Same |
| etc. | Same |

---

## Key Behavioral Differences to Validate

1. **Case sensitivity**: Newtonsoft is case-insensitive by default; System.Text.Json is case-sensitive. The deserializer must configure `PropertyNameCaseInsensitive = true` or queries will break.
2. **`DateParseHandling.None`**: Explicitly set in `CypherJsonDeserializer` — System.Text.Json does not auto-parse dates by default (closer to this behavior, but `JsonTextReader` specific options disappear).
3. **`MissingMemberHandling.Ignore`**: System.Text.Json ignores missing members by default — no change needed.
4. **`JToken` DOM tree**: The entire `CommonDeserializerMethods` traversal logic uses `JToken` extensively. This is the highest-risk area — must be ported to `JsonElement`/`JsonNode`.
5. **`NullValueHandling.Ignore`**: Equivalent is `JsonIgnoreCondition.WhenWritingNull` on `JsonSerializerOptions`.
6. **Contract resolver / runtime renames**: `Neo4jContractResolver` supports runtime property renaming — no direct equivalent in STJ without a custom converter.

---

## Migration Complexity Score

| Category | Effort |
|---|---|
| Public API breaking changes | 🔴 High — This is a library; consumers will break |
| JToken DOM rewrite | 🔴 High — Deep usage in 4+ files |
| Custom converters rewrite | 🟡 Medium — 5 converters, straightforward patterns |
| Contract resolver rewrite | 🟡 Medium — Neo4jIgnore + runtime renames |
| Attribute swaps | 🟢 Low — Mechanical find/replace |
| Test updates | 🟢 Low — Mostly follow production code |

**Estimated total effort**: Large (~3–5 days for a careful migration with full test coverage)

---

## Recommendation

Before proceeding, note this critical consideration:

> **`IGraphClient.JsonConverters` and `IGraphClient.JsonContractResolver` are public API.** Changing their types from `List<Newtonsoft.Json.JsonConverter>` to `List<System.Text.Json.Serialization.JsonConverter>` and from `Newtonsoft.Json.Serialization.DefaultContractResolver` to `System.Text.Json.JsonSerializerOptions` is a **semver major breaking change**. A version bump and changelog entry will be needed.

Two approaches are possible:
1. **Full migration** (recommended): Replace all Newtonsoft types throughout, bump major version, update public API to use STJ types.
2. **Compatibility shim** (not recommended): Keep Newtonsoft as internal implementation, expose STJ types on public API — but this means maintaining both libraries and adding complexity.

This assessment recommends **Option 1: Full migration**.
