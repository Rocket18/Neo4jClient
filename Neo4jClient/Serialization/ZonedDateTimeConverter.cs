using System;
using System.Text.RegularExpressions;
using Neo4j.Driver;
using Newtonsoft.Json;

namespace Neo4jClient.Serialization
{
    public class ZonedDateTimeConverter : JsonConverter
    {
        // Matches Memgraph format: ZonedDateTime('2020-02-13T19:49:54+00:00[Etc/UTC]')
        private static readonly Regex MemgraphZonedDateTimePattern =
            new Regex(@"^ZonedDateTime\('(.+?)(?:\[.+?\])?'\)$", RegexOptions.Compiled);

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            DateTimeOffset dto;

            if (value is ZonedDateTime zdt)
                dto = zdt.As<DateTimeOffset>();
            else if (value is DateTimeOffset dateTimeOffset)
                dto = dateTimeOffset;
            else
                throw new JsonSerializationException($"Cannot serialize {value?.GetType().Name} using ZonedDateTimeConverter.");

            writer.WriteValue(dto.ToString("o"));
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.Value == null) return objectType == typeof(DateTimeOffset?) ? (DateTimeOffset?)null : (object)null;

            // Handle already-parsed DateTime/DateTimeOffset (JToken auto-parsed without DateParseHandling.None)
            // Avoids locale-specific ToString() which fails RoundtripKind parsing
            if (reader.Value is DateTimeOffset alreadyDto)
            {
                if (objectType == typeof(DateTimeOffset) || objectType == typeof(DateTimeOffset?))
                    return alreadyDto;
                return new ZonedDateTime(alreadyDto);
            }

            if (reader.Value is DateTime alreadyDt)
            {
                var dto = new DateTimeOffset(alreadyDt, alreadyDt.Kind == DateTimeKind.Local
                    ? TimeZoneInfo.Local.GetUtcOffset(alreadyDt)
                    : TimeSpan.Zero);
                if (objectType == typeof(DateTimeOffset) || objectType == typeof(DateTimeOffset?))
                    return dto;
                return new ZonedDateTime(dto);
            }

            var parsed = ParseDateTimeOffset(reader.Value.ToString());

            if (objectType == typeof(DateTimeOffset) || objectType == typeof(DateTimeOffset?))
                return parsed;

            return new ZonedDateTime(parsed);
        }

        private static DateTimeOffset ParseDateTimeOffset(string value)
        {
            var normalized = value?.Trim();

            // Handle Memgraph wrapper format:
            // ZonedDateTime('2020-02-13T19:49:54+00:00[Etc/UTC]')
            var match = MemgraphZonedDateTimePattern.Match(normalized);
            if (match.Success)
                normalized = match.Groups[1].Value;

            // Handle raw ISO string with zone id suffix:
            // 2020-02-13T19:49:54+00:00[Etc/UTC]
            // Keep offset, strip [ZoneName] because DateTimeOffset parser can't parse zone names.
            var zoneStart = normalized?.IndexOf('[') ?? -1;
            if (zoneStart >= 0)
                normalized = normalized.Substring(0, zoneStart);

            // Try with roundtrip kind first (values that include timezone offset)
            if (DateTimeOffset.TryParse(normalized, null, System.Globalization.DateTimeStyles.RoundtripKind, out var result))
                return result;

            // Fallback: no timezone info (old LocalDateTime stored data) — assume UTC
            if (DateTime.TryParse(normalized, null, System.Globalization.DateTimeStyles.AssumeUniversal, out var dt))
                return new DateTimeOffset(dt, TimeSpan.Zero);

            throw new FormatException($"Cannot parse '{value}' as DateTimeOffset. Normalized value: '{normalized}'. Expected ISO 8601 or Memgraph ZonedDateTime/LocalDateTime format.");
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(ZonedDateTime)
                || objectType == typeof(DateTimeOffset)
                || objectType == typeof(DateTimeOffset?);
        }
    }


    public class LocalDateTimeConverter : JsonConverter
    {
        // Matches Memgraph format: LocalDateTime('2020-02-13T19:49:54')
        private static readonly Regex MemgraphLocalDateTimePattern =
            new Regex(@"^LocalDateTime\('(.+?)'\)$", RegexOptions.Compiled);

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            DateTime dt;

            if (value is LocalDateTime ldt)
                dt = ldt.As<DateTime>();
            else if (value is DateTime dateTime)
                dt = dateTime;
            else
                throw new JsonSerializationException($"Cannot serialize {value?.GetType().Name} using LocalDateTimeConverter.");

            writer.WriteValue(dt.ToString("o"));
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.Value == null) return objectType == typeof(DateTime?) ? (DateTime?)null : (object)null;

            // Handle already-parsed DateTime (JToken auto-parsed without DateParseHandling.None)
            if (reader.Value is DateTime alreadyDt)
            {
                if (objectType == typeof(DateTime) || objectType == typeof(DateTime?))
                    return alreadyDt;
                return new LocalDateTime(alreadyDt);
            }

            var dt = ParseDateTime(reader.Value.ToString());

            if (objectType == typeof(DateTime) || objectType == typeof(DateTime?))
                return dt;

            return new LocalDateTime(dt);
        }

        private static DateTime ParseDateTime(string value)
        {
            // Handle Memgraph format: LocalDateTime('2020-02-13T19:49:54')
            var match = MemgraphLocalDateTimePattern.Match(value);
            if (match.Success)
                value = match.Groups[1].Value;

            return DateTime.Parse(value, null, System.Globalization.DateTimeStyles.RoundtripKind);
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(LocalDateTime)
                || objectType == typeof(DateTime)
                || objectType == typeof(DateTime?);
        }
    }

#if NET6_0_OR_GREATER
    public class DateOnlyConverter : JsonConverter
    {
        // Matches Memgraph format: Date('2024-01-15') or LocalDate('2024-01-15')
        private static readonly Regex MemgraphLocalDatePattern =
            new Regex(@"^(?:LocalDate|Date)\('(.+?)'\)$", RegexOptions.Compiled);

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            DateOnly dateOnly;

            if (value is LocalDate ld)
                dateOnly = new DateOnly(ld.Year, ld.Month, ld.Day);
            else if (value is DateOnly d)
                dateOnly = d;
            else
                throw new JsonSerializationException($"Cannot serialize {value?.GetType().Name} using DateOnlyConverter.");

            writer.WriteValue(dateOnly.ToString("yyyy-MM-dd"));
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.Value == null)
                return objectType == typeof(DateOnly?) ? (DateOnly?)null : (object)null;

            // Handle already-parsed DateTime (Newtonsoft.Json may auto-parse "2024-01-15" as DateTime)
            if (reader.Value is DateTime alreadyDt)
            {
                var result = DateOnly.FromDateTime(alreadyDt);
                return objectType == typeof(DateOnly?) ? (DateOnly?)result : (object)result;
            }

            var parsed = ParseDateOnly(reader.Value.ToString());

            return objectType == typeof(DateOnly?) ? (DateOnly?)parsed : (object)parsed;
        }

        private static DateOnly ParseDateOnly(string value)
        {
            var normalized = value?.Trim();

            // Handle Memgraph format: Date('2024-01-15') or LocalDate('2024-01-15')
            var match = MemgraphLocalDatePattern.Match(normalized);
            if (match.Success)
                normalized = match.Groups[1].Value;

            return DateOnly.Parse(normalized);
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(LocalDate)
                || objectType == typeof(DateOnly)
                || objectType == typeof(DateOnly?);
        }
    }
#endif
}
