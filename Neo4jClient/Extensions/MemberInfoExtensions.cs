using System.Linq;
using System.Reflection;
using System.Text.Json.Serialization;

namespace Neo4jClient.Extensions
{
    public static class MemberInfoExtensions
    {
        /// <summary>Gets the name of the property. If a <see cref="JsonPropertyNameAttribute"/> is attached it will use that name, otherwise the member's own name is returned.</summary>
        /// <param name="info">The <see cref="MemberInfo"/> to get the name from</param>
        /// <returns>The JSON property name for this member.</returns>
        internal static string GetNameUsingJsonProperty(this MemberInfo info)
        {
            var jsonPropertyNameAttribute = info.GetCustomAttributes(typeof(JsonPropertyNameAttribute)).FirstOrDefault() as JsonPropertyNameAttribute;

            if (jsonPropertyNameAttribute != null && !string.IsNullOrWhiteSpace(jsonPropertyNameAttribute.Name))
                return jsonPropertyNameAttribute.Name;

            return info.Name;
        }
    }
}