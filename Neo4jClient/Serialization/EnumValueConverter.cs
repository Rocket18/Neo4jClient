using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Neo4jClient.Serialization
{
    /// <summary>
    /// A <see cref="JsonConverterFactory"/> that handles serialization of all <see cref="Enum"/> types
    /// by writing/reading the enum member name as a string.
    /// </summary>
    public class EnumValueConverter : JsonConverterFactory
    {
        public override bool CanConvert(Type typeToConvert) => typeToConvert.IsEnum;

        public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
        {
            var converterType = typeof(EnumValueConverterInner<>).MakeGenericType(typeToConvert);
            return (JsonConverter)Activator.CreateInstance(converterType);
        }

        private class EnumValueConverterInner<T> : JsonConverter<T> where T : struct, Enum
        {
            public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                if (reader.TokenType == JsonTokenType.Number)
                {
                    var numericValue = reader.GetInt64();
                    return (T)Enum.ToObject(typeof(T), numericValue);
                }

                var value = reader.GetString();
                return (T)Enum.Parse(typeof(T), value);
            }

            public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
            {
                writer.WriteStringValue(value.ToString());
            }
        }
    }
}
