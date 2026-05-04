# 05-rewrite-contract-resolver: rewrite-contract-resolver

Replace `Neo4jContractResolver` (which extends `DefaultContractResolver`) with a System.Text.Json equivalent. It currently handles:
1. `[Neo4jIgnore]` attribute — skip property during serialization
2. Runtime property renaming via `renames` dictionary

For STJ, implement this as a custom `JsonSerializerOptions`-aware approach: use `[JsonIgnore]` support in a custom `JsonConverter` or a `DefaultJsonTypeInfoResolver` modifier (available in STJ 7+ / .NET 7+; for older targets, a custom resolver is needed). Since targets include `netstandard2.0`, a compatible approach must work across all TFMs.

Files:
- `Serialization/Neo4jContractSerializer.cs`
- `Serialization/DeserializationContext.cs` — replace `IContractResolver` with `JsonSerializerOptions`

**Done when**: `[Neo4jIgnore]` suppresses serialization; runtime property renaming works; no Newtonsoft types remain; compiles on all target frameworks.
