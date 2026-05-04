using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Neo4jClient.Serialization
{
    class CommonDeserializerMethods
    {
        static readonly Regex DateRegex = new Regex(@"/Date\([-]?\d+([+-]\d+)?\)/");
        static readonly Regex DateTypeNameRegex = new Regex(@"(?<=(?<quote>['""])/)Date(?=\(.*?\)/\k<quote>)");

        public static string RemoveResultsFromJson(string content)
        {
            var root = JsonNode.Parse(content);
            var errors = root?["errors"] as JsonArray;
            if (errors != null && errors.Count > 0)
                throw DeserializeNeo4jError(errors);
            var results = root?["results"] as JsonArray;
            return results != null && results.Count > 0 ? results[0]?.ToJsonString() : null;
        }

        public static NeoException DeserializeNeo4jError(JsonArray errors) =>
            new NeoException(new ApiModels.ExceptionResponse
            {
                Exception = errors[0]?["code"]?.GetValue<string>(),
                Message = errors[0]?["message"]?.GetValue<string>(),
            });

        public static string ReplaceAllDateInstancesWithNeoDates(string content)
        {
            // Replace all /Date(1234+0200)/ instances with /NeoDate(1234+0200)/
            return DateTypeNameRegex.Replace(content, "NeoDate");
        }

        private static readonly Regex SingleQuotedJsonPattern = new Regex(@"'[^']*'\s*[:,\[\{]|'[^']*'\s*$", RegexOptions.Compiled);

        /// <summary>
        /// Converts single-quoted JSON (legacy test fixtures / Newtonsoft-lenient style) to
        /// standard double-quoted JSON that STJ can parse. Only applies when heuristic detects
        /// single-quoted keys/values are in use (to avoid corrupting embedded Cypher apostrophes).
        /// </summary>
        public static string NormalizeSingleQuotedJson(string content)
        {
            if (string.IsNullOrEmpty(content)) return content;
            if (!content.Contains('\'')) return content;
            // Heuristic: single-quoted JSON has patterns like 'key': or 'value', or 'value']
            if (SingleQuotedJsonPattern.IsMatch(content))
                return content.Replace("'", "\"");
            return content;
        }

                public static DateTimeOffset? ParseDateTimeOffset(JsonNode value)
                {
                    if (value == null) return null;
                    var rawValue = value.AsString();
                    if (string.IsNullOrWhiteSpace(rawValue)) return null;
                    rawValue = rawValue.Replace("NeoDate", "Date");
                    if (!DateRegex.IsMatch(rawValue))
                    {
                        if (!DateTimeOffset.TryParse(rawValue, null, System.Globalization.DateTimeStyles.RoundtripKind, out var parsed))
                            return null;
                        return parsed;
                    }
                    // /Date(ticks)/ or /Date(ticks+HHMM)/ or /Date(ticks-HHMM)/
                    var fullMatch = System.Text.RegularExpressions.Regex.Match(rawValue, @"/Date\(([-]?\d+)([+-]\d{4})?\)/");
                    if (fullMatch.Success && long.TryParse(fullMatch.Groups[1].Value, out var ticks))
                    {
                        var utc = DateTimeOffset.FromUnixTimeMilliseconds(ticks);
                        if (fullMatch.Groups[2].Success)
                        {
                            var offsetStr = fullMatch.Groups[2].Value; // e.g. "+0200" or "-0500"
                            var sign = offsetStr[0] == '-' ? -1 : 1;
                            var hours = int.Parse(offsetStr.Substring(1, 2));
                            var minutes = int.Parse(offsetStr.Substring(3, 2));
                            var offset = new TimeSpan(sign * hours, sign * minutes, 0);
                            return new DateTimeOffset(utc.DateTime + offset, offset);
                        }
                        return utc;
                    }
                    return null;
                }

                public static DateTime? ParseDateTime(JsonNode value)
        {
            var rawValue = value.AsString();
            if (string.IsNullOrWhiteSpace(rawValue)) return null;
            rawValue = rawValue.Replace("NeoDate", "Date");
            if (!DateRegex.IsMatch(rawValue))
            {
                if (!DateTime.TryParse(rawValue, null, System.Globalization.DateTimeStyles.RoundtripKind, out var parsed)) return null;
                return parsed;
            }
            var ticksMatch = System.Text.RegularExpressions.Regex.Match(rawValue, @"/Date\(([-]?\d+)(?:[+-]\d+)?\)/");
            if (ticksMatch.Success && long.TryParse(ticksMatch.Groups[1].Value, out var ticks))
                return DateTimeOffset.FromUnixTimeMilliseconds(ticks).UtcDateTime;
            return null;
        }

        public static object CoerceValue(DeserializationContext context, PropertyInfo propertyInfo, JsonNode value, IEnumerable<TypeMapping> typeMappings, int nestingLevel)
        {
            if (value == null) return null;

            var propertyType = propertyInfo.PropertyType;
            var typeInfo = propertyType.GetTypeInfo();
            if (TryJsonConverters(context, propertyType, value, out var jsonConversionResult))
                return jsonConversionResult;
            
            Type genericTypeDef = null;

            if (typeInfo.IsGenericType)
            {
                genericTypeDef = propertyType.GetGenericTypeDefinition();

                if (genericTypeDef == typeof(Nullable<>))
                {
                    propertyType = propertyType.GetGenericArguments()[0];
                    genericTypeDef = null;
                }
            }

            typeMappings = typeMappings.ToArray();
            if (typeInfo.IsPrimitive)
            {
                object tmpVal = value.AsString().Replace("\"", string.Empty);
                tmpVal = Convert.ChangeType(tmpVal, propertyType);
                return tmpVal;
            }

            if (typeInfo.IsEnum)
                return Enum.Parse(propertyType, value.AsString(), false);

            if (propertyType == typeof(Uri))
                return new Uri(value.AsString(), UriKind.RelativeOrAbsolute);

            if (propertyType == typeof(string))
                return value.AsString();

            if (propertyType == typeof(DateTime))
                return ParseDateTime(value);

            if (propertyType == typeof(DateTimeOffset))
                return ParseDateTimeOffset(value);

            if (propertyType == typeof(decimal))
            {
                if (value is JsonValue jvDec) return jvDec.GetValue<decimal>();
                return decimal.Parse(value.AsString());
            }

            if (propertyType == typeof(TimeSpan))
                return TimeSpan.Parse(value.AsString());

            if (propertyType == typeof(Guid))
            {
                var raw = value.AsString();
                return string.IsNullOrEmpty(raw) ? Guid.Empty : new Guid(raw);
            }

            if (propertyType == typeof(byte[]))
                return Convert.FromBase64String(value.AsString());

            if (genericTypeDef == typeof(List<>))
                return BuildList(context, propertyType, value.AsArray(), typeMappings, nestingLevel + 1);

            if (genericTypeDef == typeof(Dictionary<,>))
            {
                if (propertyType.GetGenericArguments()[0] != typeof(string))
                    throw new NotSupportedException("Value coercion only supports dictionaries with a key of type System.String");
                return BuildDictionary(context, propertyType, value.AsObject(), typeMappings, nestingLevel + 1);
            }

            var mapping = typeMappings.FirstOrDefault(m => m.ShouldTriggerForPropertyType(nestingLevel, propertyType));
            return mapping != null
                ? MutateObject(context, value, typeMappings, nestingLevel, mapping, propertyType)
                : CreateAndMap(context, propertyType, value, typeMappings, nestingLevel + 1);
        }

        public static void SetPropertyValue(DeserializationContext context, object targetObject, PropertyInfo propertyInfo, JsonNode value, IEnumerable<TypeMapping> typeMappings, int nestingLevel)
        {
            if (value == null) return;
            var coercedValue = CoerceValue(context, propertyInfo, value, typeMappings, nestingLevel);
            propertyInfo.SetValue(targetObject, coercedValue, null);
        }

        public static object CreateAndMap(DeserializationContext context, Type type, JsonNode element, IEnumerable<TypeMapping> typeMappings, int nestingLevel)
        {
            if (element == null) return null;

            object instance;
            typeMappings = typeMappings.ToArray();

            Type genericTypeDefinition = null;
            var typeInfo = type.GetTypeInfo();
            if (typeInfo.IsGenericType)
            {
                genericTypeDefinition = type.GetGenericTypeDefinition();
                if (genericTypeDefinition == typeof(Nullable<>))
                {
                    type = type.GetGenericArguments()[0];
                    genericTypeDefinition = null;
                }
            }

            if (genericTypeDefinition != null)
            {
                if (genericTypeDefinition == typeof(Dictionary<,>))
                    instance = BuildDictionary(context, type, element.AsObject(), typeMappings, nestingLevel + 1);
                else if (genericTypeDefinition == typeof(List<>))
                    instance = BuildList(context, type, element.AsArray(), typeMappings, nestingLevel + 1);
                else if (genericTypeDefinition == typeof(IEnumerable<>))
                    instance = BuildIEnumerable(context, type, element.AsArray(), typeMappings, nestingLevel + 1);
                else if (type == typeof(string))
                    instance = element.AsString();
                else
                {
                    var mapping = typeMappings.FirstOrDefault(m => m.ShouldTriggerForPropertyType(nestingLevel, type));
                    if (mapping != null)
                        instance = MutateObject(context, element, typeMappings, nestingLevel, mapping, type);
                    else
                    {
                        instance = Activator.CreateInstance(type);
                        Map(context, instance, element, typeMappings, nestingLevel);
                    }
                }
            }
            else if (type == typeof(byte[]))
                instance = Convert.FromBase64String(element.AsString());
            else if (typeInfo.BaseType == typeof(Array))
            {
                var underlyingType = type.GetElementType();
                instance = BuildArray(context, typeof(ArrayList), underlyingType, element.AsArray(), typeMappings, nestingLevel + 1);
            }
            else if (type == typeof(string))
                instance = element.AsString();
            else if (TryJsonConverters(context, type, element, out instance))
            {
            }
            else if (typeInfo.IsValueType)
            {
                if (type == typeof(Guid))
                    instance = Guid.Parse(element.AsString());
                else if (typeInfo.BaseType == typeof(Enum))
                    instance = Enum.Parse(type, element.AsString(), false);
                else
                    instance = Convert.ChangeType(element.AsString(), type);
            }
            else if (type == typeof(object))
            {
                if (element is JsonValue jv)
                    instance = UnwrapJsonValue(jv);
                else
                    instance = element;
            }
            else
            {
                try
                {
                    instance = Activator.CreateInstance(type);
                }
                catch (MissingMethodException ex)
                {
                    throw new DeserializationException(
                        $"We expected a default public constructor on {type.Name} so that we could create instances of it to deserialize data into, however this constructor does not exist or is inaccessible.",
                        ex);
                }
                Map(context, instance, element, typeMappings, nestingLevel);
            }
            return instance;
        }

        [ThreadStatic]
        private static bool _inTryJsonConverters;

        static bool TryJsonConverters(DeserializationContext context, Type type, JsonNode element, out object instance)
        {
            instance = null;
            if (context.JsonConverters == null || context.JsonConverters.Length == 0) return false;
            var converter = context.JsonConverters.FirstOrDefault(c => c.CanConvert(type));
            if (converter == null) return false;
            if (_inTryJsonConverters) return false;   // prevent recursive re-entry
            var json = element?.ToJsonString() ?? "null";
            // Build options that include the registered converters so they are available during deserialization
            var options = new JsonSerializerOptions(context.JsonSerializerOptions ?? new JsonSerializerOptions());
            foreach (var c in context.JsonConverters)
                if (!options.Converters.Contains(c))
                    options.Converters.Add(c);
            _inTryJsonConverters = true;
            try
            {
                instance = JsonSerializer.Deserialize(json, type, options);
                return true;
            }
            catch { return false; }
            finally { _inTryJsonConverters = false; }
        }
        static object MutateObject(DeserializationContext context, JsonNode value, IEnumerable<TypeMapping> typeMappings, int nestingLevel,
                                   TypeMapping mapping, Type propertyType)
        {
            var newType = mapping.DetermineTypeToParseJsonIntoBasedOnPropertyType(propertyType);
            var rawItem = CreateAndMap(context, newType, value, typeMappings, nestingLevel + 1);
            return mapping.MutationCallback(rawItem);
        }

        public static Dictionary<string, PropertyInfo> ApplyPropertyCasing(DeserializationContext context, Dictionary<string, PropertyInfo> properties)
        {
            if (context.JsonSerializerOptions?.PropertyNamingPolicy == JsonNamingPolicy.CamelCase)
            {
                var camel = new Func<string, string>(name => string.Format("{0}{1}", name.Substring(0,1).ToLowerInvariant(), name.Length > 1 ? name.Substring(1) : string.Empty));
                return properties.Select(x => new { Key = camel(x.Key), x.Value }).ToDictionary(x => x.Key, x => x.Value);
            }
            return properties;
        }

        public static void Map(DeserializationContext context, object targetObject, JsonNode parentJsonNode, IEnumerable<TypeMapping> typeMappings, int nestingLevel)
        {
            typeMappings = typeMappings.ToArray();
            var objType = targetObject.GetType();
            var props = GetPropertiesForType(context, objType);

            if (!(parentJsonNode is JsonObject parentObj))
                return;

            // Unwrap "data" wrapper if expected property keys are missing
            if (props.Keys.Any(k => !parentObj.ContainsKey(k)) && parentObj.ContainsKey("data"))
                parentObj = parentObj["data"]?.AsObject() ?? parentObj;

            foreach (var propertyName in props.Keys)
            {
                var propertyInfo = props[propertyName];
                var jsonToken = parentObj.ContainsKey(propertyName) ? parentObj[propertyName] : null;
                SetPropertyValue(context, targetObject, propertyInfo, jsonToken, typeMappings, nestingLevel);
            }
        }

        /// <summary>
        /// Unwraps a <see cref="JsonValue"/> to the most natural CLR primitive type.
        /// </summary>
        private static object UnwrapJsonValue(JsonValue jv)
        {
            // Try common primitives in order of specificity
            if (jv.TryGetValue<bool>(out var boolVal)) return boolVal;
            if (jv.TryGetValue<long>(out var longVal)) return longVal;
            if (jv.TryGetValue<double>(out var dblVal)) return dblVal;
            if (jv.TryGetValue<string>(out var strVal)) return strVal;
            // Fallback: return the raw object (may be JsonElement for edge cases)
            if (jv.TryGetValue<object>(out var objVal)) return objVal;
            return jv;
        }

        public static IDictionary BuildDictionary(DeserializationContext context, Type type, JsonObject elements, IEnumerable<TypeMapping> typeMappings, int nestingLevel)
        {
            typeMappings = typeMappings.ToArray();
            var dict = (IDictionary)Activator.CreateInstance(type);
            var valueType = type.GetGenericArguments()[1];
            foreach (var kvp in elements)
            {
                var item = CreateAndMap(context, valueType, kvp.Value, typeMappings, nestingLevel + 1);
                dict.Add(kvp.Key, item);
            }

            return dict;
        }

        public static IList BuildList(DeserializationContext context, Type type, JsonArray elements, IEnumerable<TypeMapping> typeMappings, int nestingLevel)
        {
            typeMappings = typeMappings.ToArray();
            var list = (IList)Activator.CreateInstance(type);
            var itemType = type
                .GetInterfaces()
                .Where(i => i.GetTypeInfo().IsGenericType && i.GetGenericTypeDefinition() == typeof(IList<>))
                .Select(i => i.GetGenericArguments().First())
                .Single();

            foreach (var element in elements)
            {
                if (itemType.GetTypeInfo().IsPrimitive)
                {
                    if (element is JsonValue jv)
                    {
                        var raw = jv.GetValue<object>();
                        list.Add(Convert.ChangeType(raw.ToString(), itemType));
                    }
                }
                else if (itemType == typeof(string))
                    list.Add(element.AsString());
                else
                    list.Add(CreateAndMap(context, itemType, element, typeMappings, nestingLevel + 1));
            }
            return list;
        }

        public static Array BuildArray(DeserializationContext context, Type type, Type itemType, JsonArray elements, IEnumerable<TypeMapping> typeMappings, int nestingLevel)
        {
            typeMappings = typeMappings.ToArray();
            var list = (ArrayList)Activator.CreateInstance(type);
            foreach (var element in elements)
            {
                if (itemType.GetTypeInfo().IsPrimitive)
                {
                    if (element is JsonValue jv)
                    {
                        var raw = jv.GetValue<object>();
                        list.Add(Convert.ChangeType(raw.ToString(), itemType));
                    }
                }
                else if (itemType == typeof(string))
                    list.Add(element.AsString());
                else
                    list.Add(CreateAndMap(context, itemType, element, typeMappings, nestingLevel + 1));
            }
            return list.ToArray(itemType);
        }

        public static IList BuildIEnumerable(DeserializationContext context, Type type, JsonArray elements, IEnumerable<TypeMapping> typeMappings, int nestingLevel)
        {
            typeMappings = typeMappings.ToArray();
            var itemType = type.GetGenericArguments().Single();
            var listType = typeof(List<>).MakeGenericType(itemType);
            var list = (IList)Activator.CreateInstance(listType);
            foreach (var element in elements)
            {
                if (itemType.GetTypeInfo().IsPrimitive)
                {
                    if (element is JsonValue jv)
                        list.Add(Convert.ChangeType(jv.GetValue<object>(), itemType));
                }
                else if (itemType == typeof(string))
                    list.Add(element.AsString());
                else
                    list.Add(CreateAndMap(context, itemType, element, typeMappings, nestingLevel + 1));
            }
            return list;
        }

        static readonly Dictionary<Type, Dictionary<string, PropertyInfo>> PropertyInfoCache = new Dictionary<Type, Dictionary<string, PropertyInfo>>();
        static readonly object PropertyInfoCacheLock = new object();
        static Dictionary<string, PropertyInfo> GetPropertiesForType(DeserializationContext context, Type objType)
        {
            if (PropertyInfoCache.TryGetValue(objType, out var cached))
                return cached;

            lock (PropertyInfoCacheLock)
            {
                if (PropertyInfoCache.TryGetValue(objType, out cached))
                    return cached;

                var useCamelCase = context.JsonSerializerOptions?.PropertyNamingPolicy == JsonNamingPolicy.CamelCase;
                var camel = new Func<string, string>(name => string.Format("{0}{1}",
                    name.Substring(0, 1).ToLowerInvariant(),
                    name.Length > 1 ? name.Substring(1) : string.Empty));

                var properties = objType
                    .GetProperties()
                    .Where(p => p.CanWrite)
                    .Select(p =>
                    {
                        var attr = p.GetCustomAttribute<JsonPropertyNameAttribute>();
                        var name = attr?.Name ?? (useCamelCase ? camel(p.Name) : p.Name);
                        return new { Name = name, Property = p };
                    });

                var result = properties.ToDictionary(p => p.Name, p => p.Property);
                PropertyInfoCache[objType] = result;
                return result;
            }
        }
    }
}
