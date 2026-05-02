using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Neo4jClient.Serialization
{
    public class CustomJsonDeserializer
    {
        readonly IEnumerable<JsonConverter> jsonConverters;
        readonly CultureInfo culture;
        readonly JsonSerializerOptions jsonOptions;

        public CustomJsonDeserializer(IEnumerable<JsonConverter> jsonConverters) : this(jsonConverters, null)
        {
        }

        public CustomJsonDeserializer(IEnumerable<JsonConverter> jsonConverters, CultureInfo cultureInfo = null, JsonSerializerOptions options = null)
        {
            this.jsonConverters = jsonConverters;
            culture = cultureInfo ?? CultureInfo.InvariantCulture;
            jsonOptions = options ?? GraphClient.DefaultJsonSerializerOptions;
        }

        public T Deserialize<T>(string content) where T : new()
        {
            var context = new DeserializationContext
            {
                Culture = culture,
                JsonConverters = (jsonConverters ?? new List<JsonConverter>(0)).Reverse().ToArray(),
                JsonSerializerOptions = jsonOptions
            };

            content = CommonDeserializerMethods.ReplaceAllDateInstancesWithNeoDates(content);
            var root = JsonNode.Parse(content);
            var target = new T();

            if (target is IList)
            {
                var objType = target.GetType();
                target = (T)CommonDeserializerMethods.BuildList(context, objType, root.AsArray(), new TypeMapping[0], 0);
            }
            else if (target is IDictionary)
            {
                target = (T)CommonDeserializerMethods.BuildDictionary(context, target.GetType(), root.AsObject(), new TypeMapping[0], 0);
            }
            else
            {
                CommonDeserializerMethods.Map(context, target, root, new TypeMapping[0], 0);
            }

            return target;
        }
    }
}
