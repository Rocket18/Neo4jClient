using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Neo4jClient.Serialization
{
    /// <summary>
    /// A <see cref="JsonConverterFactory"/> that handles types which have a <see cref="TypeConverter"/>
    /// that can convert to/from <see cref="string"/>. Used for custom value types that are not
    /// primitive or one of the well-known built-in types.
    /// </summary>
    public class TypeConverterBasedJsonConverter : JsonConverterFactory
    {
        internal static readonly Type[] BuiltinTypes =
        {
            typeof(string),
            typeof(bool),
            typeof(bool?),
            typeof(byte),
            typeof(byte?),
            typeof(char),
            typeof(char?),
            typeof(double),
            typeof(double?),
            typeof(short),
            typeof(short?),
            typeof(ushort),
            typeof(ushort?),
            typeof(int),
            typeof(int?),
            typeof(uint),
            typeof(uint?),
            typeof(long),
            typeof(long?),
            typeof(ulong),
            typeof(ulong?),
            typeof(SByte),
            typeof(SByte?),
            typeof(Single),
            typeof(Single?),
            typeof(Uri),
            typeof(DateTime),
            typeof(DateTime?),
            typeof(DateTimeOffset),
            typeof(DateTimeOffset?),
            typeof(decimal),
            typeof(decimal?),
            typeof(TimeSpan),
            typeof(TimeSpan?),
            typeof(Guid),
            typeof(Guid?)
        };

        public override bool CanConvert(Type typeToConvert)
        {
            var typeConverter = TypeDescriptor.GetConverter(typeToConvert);
            return !typeToConvert.GetTypeInfo().IsPrimitive &&
                   !BuiltinTypes.Contains(typeToConvert) &&
                   typeConverter.GetType() != typeof(TypeConverter) &&
                   typeConverter.CanConvertTo(typeof(string)) &&
                   typeConverter.CanConvertFrom(typeof(string));
        }

        public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
        {
            var converterType = typeof(TypeConverterBasedJsonConverterInner<>).MakeGenericType(typeToConvert);
            return (JsonConverter)Activator.CreateInstance(converterType);
        }

        private class TypeConverterBasedJsonConverterInner<T> : JsonConverter<T>
        {
            public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                if (reader.TokenType == JsonTokenType.Null) return default;
                var stringValue = reader.GetString();
                var typeConverter = TypeDescriptor.GetConverter(typeof(T));
                return (T)typeConverter.ConvertFromString(stringValue);
            }

            public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
            {
                if (value == null)
                {
                    writer.WriteNullValue();
                    return;
                }
                var typeConverter = TypeDescriptor.GetConverter(typeof(T));
                writer.WriteStringValue(typeConverter.ConvertToInvariantString(value));
            }
        }
    }
}
