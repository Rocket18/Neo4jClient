using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace Neo4jClient.Serialization
{
    /// <summary>
    /// Builds a <see cref="JsonSerializerOptions"/> instance that:
    ///  1. Skips properties decorated with <see cref="Neo4jIgnoreAttribute"/>.
    ///  2. Applies runtime property renames registered via <see cref="AddPropertyMapping"/>.
    ///  3. Ignores null values on write (mirrors original NullValueHandling.Ignore default).
    /// </summary>
    public class Neo4jContractResolver
    {
        private readonly Dictionary<Type, Dictionary<string, string>> _renames
            = new Dictionary<Type, Dictionary<string, string>>();

        public void AddPropertyMapping(Type type, string originalName, string newName)
        {
            if (!_renames.TryGetValue(type, out var dict))
            {
                dict = new Dictionary<string, string>();
                _renames[type] = dict;
            }
            dict[originalName] = newName;
        }

        /// <summary>
        /// Builds a <see cref="JsonSerializerOptions"/> that encodes the Neo4j-specific
        /// ignore and rename behaviour. Callers should cache the result where possible.
        /// </summary>
        public JsonSerializerOptions BuildOptions(
            IEnumerable<System.Text.Json.Serialization.JsonConverter> extraConverters = null,
            bool camelCase = false,
            bool ignoreNulls = true)
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                DefaultIgnoreCondition = ignoreNulls
                    ? JsonIgnoreCondition.WhenWritingNull
                    : JsonIgnoreCondition.Never,
                PropertyNamingPolicy = camelCase ? JsonNamingPolicy.CamelCase : null,
            };

            // Register custom converters first
            if (extraConverters != null)
            {
                foreach (var c in extraConverters)
                    options.Converters.Add(c);
            }

            // Apply Neo4jIgnore + runtime renames via a type-info modifier
            options.TypeInfoResolver = new DefaultJsonTypeInfoResolver
            {
                Modifiers = { ApplyNeo4jModifier }
            };

            return options;
        }

        private void ApplyNeo4jModifier(JsonTypeInfo typeInfo)
        {
            if (typeInfo.Kind != JsonTypeInfoKind.Object) return;

            _renames.TryGetValue(typeInfo.Type, out var renameMap);

            foreach (var prop in typeInfo.Properties)
            {
                // 1. Skip [Neo4jIgnore] properties
                var clrProp = typeInfo.Type.GetProperty(prop.Name,
                    BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                if (clrProp != null && clrProp.GetCustomAttribute<Neo4jIgnoreAttribute>() != null)
                {
                    prop.ShouldSerialize = (obj, val) => false;
#if NET8_0_OR_GREATER
                    prop.ShouldDeserialize = _ => false;
#endif
                    continue;
                }

                // 2. Apply runtime rename
                if (renameMap != null && renameMap.TryGetValue(prop.Name, out var newName))
                    prop.Name = newName;
            }
        }
    }
}
