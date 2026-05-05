using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Neo4j.Driver;
using Neo4jClient.Cypher;
using Neo4jClient.Serialization;

namespace Neo4jClient
{
    public static class Neo4jDriverExtensions
    {
        private const string DefaultDateTimeFormat = "yyyy'-'MM'-'dd'T'HH':'mm':'ss.FFFFFFFK";
        private const string DefaultTimeSpanFormat = @"d\.hh\:mm\:ss\.fffffff";

        
        public static Task<IResultCursor> Run(this IAsyncSession session, CypherQuery query, IGraphClient gc)
        {
            return session.RunAsync(query.QueryText, query.ToNeo4jDriverParameters(gc));
        }

        public static Task<IResultCursor> RunAsync(this IAsyncTransaction transaction, CypherQuery query, IGraphClient gc)
        {
            return transaction.RunAsync(query.QueryText, query.ToNeo4jDriverParameters(gc));
        }

        public static Task<IResultCursor> RunAsync(this IAsyncQueryRunner queryRunner, CypherQuery query, IGraphClient gc)
        {
            return queryRunner.RunAsync(query.QueryText, query.ToNeo4jDriverParameters(gc));
        }

        // public static IStatementResult Run(this ITransaction transaction, CypherQuery query, IGraphClient gc)
        // {
        //     return transaction.Run(query.QueryText, query.ToNeo4jDriverParameters(gc));
        // }
        //
        // public static async Task<IResultCursor> RunAsync(this IAsyncSession session, CypherQuery query, IGraphClient gc)
        // {
        //     return await session.RunAsync(query.QueryText, query.ToNeo4jDriverParameters(gc)).ConfigureAwait(false);
        // }
        //
        // public static async Task<IResultCursor> RunAsync(this ITransaction session, CypherQuery query, IGraphClient gc)
        // {
        //     return await session.RunAsync(query.QueryText, query.ToNeo4jDriverParameters(gc)).ConfigureAwait(false);
        // }

        // ReSharper disable once InconsistentNaming
        public static Dictionary<string, object> ToNeo4jDriverParameters(this CypherQuery query, IGraphClient gc)
        {
            return query.QueryParameters.ToDictionary(item => item.Key, item => Serialize(item.Value, gc.JsonConverters, gc));
        }

        // ReSharper disable once ClassNeverInstantiated.Local
        private class CustomJsonConverterHelper
        {
            // ReSharper disable once UnusedAutoPropertyAccessor.Local
            public object Value { get; set; }
        }

        private static object Serialize(object value, IList<JsonConverter> converters, IGraphClient gc, IEnumerable<CustomAttributeData> customAttributes = null)
        {
            if (value == null) return null;

            var type = value.GetType();
            var typeInfo = type.GetTypeInfo();

            // [Neo4jDateTime] on a property means pass the raw value directly to the driver
            if (customAttributes != null && customAttributes.Any(x => x.AttributeType == typeof(Neo4jDateTimeAttribute)))
            {
                return value;
            }

            // The built-in LocalDateTimeConverter and ZonedDateTimeConverter are JsonConverterFactory
            // instances that handle DateTime/DateTimeOffset for *deserialization* of Neo4j driver types.
            // When serializing parameters we must not let them intercept plain .NET DateTime/DateTimeOffset
            // values — their "o" format includes trailing zeros that break equality checks.
            // User-supplied non-factory converters (e.g. JsonConverter<DateTime>) should still be applied.
            var converter = converters?.FirstOrDefault(c =>
                c.CanConvert(type) &&
                !(c is JsonConverterFactory && IsBuiltInDateType(type)));
            if (converter != null)
            {
                try
                {
                    var serializer = new CustomJsonSerializer { JsonConverters = converters, JsonSerializerOptions = ((IRawGraphClient)gc).JsonSerializerOptions };
                    var json = serializer.Serialize(new { value });
                    var helper = JsonSerializer.Deserialize<CustomJsonConverterHelper>(json, gc.JsonSerializerOptions ?? GraphClient.DefaultJsonSerializerOptions);
                    var result = helper?.Value;
                    // STJ deserializes numbers/strings into JsonElement when the target type is object — unwrap to CLR types
                    if (result is JsonElement je)
                        return UnwrapJsonElement(je);
                    return result;
                }
                catch (NotImplementedException)
                {
                    // converter is read-only (Write throws NotImplementedException) — fall through to normal serialization
                }
            }

            if (type == typeof(DateTimeOffset))
            {
                return new ZonedDateTime((DateTimeOffset)value);
            }

            if (type == typeof(DateTime))
            {
                return new LocalDateTime((DateTime)value);
            }

#if NET6_0_OR_GREATER
            if (type == typeof(DateOnly))
            {
                var d = (DateOnly)value;
                return new LocalDate(d.Year, d.Month, d.Day);
            }
#endif

            if (typeInfo.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>))
            {
                return SerializeDictionary(type, value, converters, gc);
            }

            if (typeInfo.IsClass && type != typeof(string))
            {
                if (typeInfo.IsArray || typeInfo.ImplementedInterfaces.Contains(typeof(IEnumerable)))
                {
                    return SerializeCollection((IEnumerable)value, converters, gc);
                }

                return SerializeObject(type, value, converters, gc);
            }

            return SerializePrimitive(type, typeInfo, value);
        }

        private static bool CanHandleNativeDateTimeType(Type type)
        {
            return type == typeof(DateTime) || type == typeof(DateTimeOffset) || type == typeof(TimeSpan);
        }

        private static bool IsBuiltInDateType(Type type)
        {
            if (type == typeof(DateTime) || type == typeof(DateTimeOffset) ||
                type == typeof(DateTime?) || type == typeof(DateTimeOffset?))
                return true;
#if NET6_0_OR_GREATER
            if (type == typeof(DateOnly) || type == typeof(DateOnly?))
                return true;
#endif
            return false;
        }

        private static object UnwrapJsonElement(JsonElement je)
        {
            switch (je.ValueKind)
            {
                case JsonValueKind.True:
                    return true;
                case JsonValueKind.False:
                    return false;
                case JsonValueKind.String:
                    return je.GetString();
                case JsonValueKind.Number:
                    long l;
                    if (je.TryGetInt64(out l))
                        return l;
                    return je.GetDouble();
                default:
                    return je;
            }
        }

        private static object SerializeObject(Type type, object value, IList<JsonConverter> converters, IGraphClient gc)
        {
            var serialized = new Dictionary<string, object>();
            foreach (var propertyInfo in type.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(pi => !(pi.GetIndexParameters().Any() || pi.IsDefined(typeof(JsonIgnoreAttribute)) || pi.IsDefined(typeof(Neo4jIgnoreAttribute)))))
            {
                var propertyName = GetPropertyName(propertyInfo, gc);
                var propertyValue = propertyInfo.GetValue(value);
                if (propertyValue != null || gc.ExecutionConfiguration.SerializeNullValues)
                    serialized.Add(propertyName, Serialize(propertyValue, converters, gc, propertyInfo.CustomAttributes));
            }
            return serialized;
        }

        private static string GetPropertyName(PropertyInfo propertyInfo, IGraphClient gc)
        {
            // Check [JsonPropertyName] first
            var attr = propertyInfo.GetCustomAttribute<System.Text.Json.Serialization.JsonPropertyNameAttribute>();
            if (attr != null) return attr.Name;

            // Fall back to JsonSerializerOptions naming policy (e.g. camelCase)
            var policy = gc?.JsonSerializerOptions?.PropertyNamingPolicy;
            return policy != null ? policy.ConvertName(propertyInfo.Name) : propertyInfo.Name;
        }

        private static object SerializeCollection(IEnumerable value, IList<JsonConverter> converters, IGraphClient gc)
        {
            return value.Cast<object>().Select(x => Serialize(x, converters, gc)).ToArray();
        }

        private static object SerializePrimitive(Type type, TypeInfo typeInfo, object instance)
        {
            if (type == typeof(DateTime))
            {
                return new LocalDateTime((DateTime)instance);
            }

            if (type == typeof(DateTimeOffset))
            {
                return new ZonedDateTime((DateTimeOffset)instance);
            }

#if NET6_0_OR_GREATER
            if (type == typeof(DateOnly))
            {
                var d = (DateOnly)instance;
                return new LocalDate(d.Year, d.Month, d.Day);
            }
#endif

            if (type == typeof(TimeSpan))
            {
                return SerializeTimeSpan((TimeSpan) instance);
            }

            if (type == typeof(string) || typeInfo.IsPrimitive || type == typeof(decimal))
            {
                return instance;
            }
            
            if (type == typeof(Guid))
            {
                return $"{instance}";
            }

            // last case scenario serialize it as JSON
            return JsonSerializer.Serialize(instance);
        }

        private static LocalDateTime SerializeDateTime(DateTime dateTime)
        {
            return new LocalDateTime(dateTime);
        }

        private static ZonedDateTime SerializeDateTimeOffset(DateTimeOffset dateTime)
        {
            return new ZonedDateTime(dateTime);
        }

        private static string SerializeTimeSpan(TimeSpan timeSpan)
        {
            return timeSpan.ToString(DefaultTimeSpanFormat, CultureInfo.CurrentCulture);
        }

        private static object SerializeDictionary(Type type, object value, IList<JsonConverter> converters, IGraphClient gc)
        {
            var keyType = type.GetGenericArguments()[0];
            if (keyType != typeof(string))
                throw new NotSupportedException($"Dictionary had keys with type '{keyType.Name}'. Only dictionaries with type 'String' are supported.");

            var serialized = new Dictionary<string, object>();
            foreach (var item in (dynamic)value)
            {
                string key = item.Key;
                object entry = item.Value;
                if (entry != null || gc.ExecutionConfiguration.SerializeNullValues)
                    serialized[key] = Serialize(entry, converters, gc);
            }
            return serialized;
        }
    }
}