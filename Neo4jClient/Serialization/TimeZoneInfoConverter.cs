using System;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Neo4jClient.Serialization
{
    public class TimeZoneInfoConverter : JsonConverter<TimeZoneInfo>
    {
        public override TimeZoneInfo Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var value = reader.GetString();
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(value);
            }
            catch
            {
                Debug.WriteLine("Could not deserialize TimeZoneInfo, defaulting to Utc. Ensure the TimeZoneId is valid. Valid TimeZone Ids are:");
                foreach (var timeZone in TimeZoneInfo.GetSystemTimeZones())
                {
                    Debug.WriteLine(timeZone.Id);
                }
                return TimeZoneInfo.Utc;
            }
        }

        public override void Write(Utf8JsonWriter writer, TimeZoneInfo value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.Id);
        }
    }
}
