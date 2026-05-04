# 03-rewrite-custom-converters: rewrite-custom-converters

Rewrite the five custom Newtonsoft `JsonConverter` subclasses as `System.Text.Json.Serialization.JsonConverter<T>` implementations. Each converter must preserve its existing serialization/deserialization behavior exactly.

Files:
- `Serialization/EnumValueConverter.cs`
- `Serialization/NullableEnumValueConverter.cs`
- `Serialization/TimeZoneInfoConverter.cs`
- `Serialization/TypeConverterBasedJsonConverter.cs`
- `Serialization/ZonedDateTimeConverter.cs`

Key differences to handle: `JsonConverter<T>` is generic (separate converters per type or use factory pattern); `Read`/`Write` use `Utf8JsonReader`/`Utf8JsonWriter` instead of `JsonReader`/`JsonWriter`; `CanConvert` is replaced by the generic type parameter or a `JsonConverterFactory`.

**Done when**: All five converters compile using only `System.Text.Json` types; their logic is functionally equivalent to the originals; no Newtonsoft imports remain.
