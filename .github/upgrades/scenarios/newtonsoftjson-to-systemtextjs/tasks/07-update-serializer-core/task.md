# 07-update-serializer-core: update-serializer-core

Update `CustomJsonSerializer` internals to use `JsonSerializer` from `System.Text.Json` for actual serialization/deserialization. Wire up `JsonSerializerOptions` (converters, naming policy, null handling) to replace the old `JsonSerializer` + `JsonTextWriter` + `IContractResolver` pipeline.

Configure options to preserve existing behavior:
- `PropertyNameCaseInsensitive = true`
- `DefaultIgnoreCondition = WhenWritingNull` (mirrors `NullValueHandling.Ignore`)
- Register all custom converters from task 03

**Done when**: `CustomJsonSerializer.Serialize` and `Deserialize<T>` work correctly using STJ; options are wired to existing converter list and renamed properties.
