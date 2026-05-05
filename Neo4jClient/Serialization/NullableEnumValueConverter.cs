using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Neo4jClient.Serialization
{
    /// <summary>
    /// A <see cref="JsonConverterFactory"/> that handles serialization of <see cref="Nullable{T}"/> enum types
    /// by writing/reading the enum member name as a string, or null.
    /// </summary>
    public class NullableEnumValueConverter : JsonConverterFactory
    {
        public override bool CanConvert(Type typeToConvert)
        {
            var underlying = Nullable.GetUnderlyingType(typeToConvert);
            return underlying != null && underlying.IsEnum;
        }

        public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
        {
            var enumType = Nullable.GetUnderlyingType(typeToConvert);
            var converterType = typeof(NullableEnumValueConverterInner<>).MakeGenericType(enumType);
            return (JsonConverter)Activator.CreateInstance(converterType);
        }

        private class NullableEnumValueConverterInner<T> : JsonConverter<T?> where T : struct, Enum
        {
            public override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                var value = reader.GetString();
                if (string.IsNullOrEmpty(value))
                    return null;
                return (T)Enum.Parse(typeof(T), value);
            }

            public override void Write(Utf8JsonWriter writer, T? value, JsonSerializerOptions options)
            {
                if (value == null)
                    writer.WriteNullValue();
                else
                    writer.WriteStringValue(value.ToString());
            }
        }
    }
}
