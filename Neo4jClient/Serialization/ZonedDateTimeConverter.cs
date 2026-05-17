using System;
using System.Text.RegularExpressions;
using System.Text.Json;
using System.Text.Json.Serialization;
using Neo4j.Driver;

namespace Neo4jClient.Serialization
{
    public class ZonedDateTimeConverter : JsonConverterFactory
    {
        // Matches Memgraph format: ZonedDateTime('2020-02-13T19:49:54+00:00[Etc/UTC]')
        private static readonly Regex MemgraphZonedDateTimePattern =
            new Regex(@"^ZonedDateTime\('(.+?)(?:\[.+?\])?'\)$", RegexOptions.Compiled);

        public override bool CanConvert(Type typeToConvert)
            => typeToConvert == typeof(ZonedDateTime)
            || typeToConvert == typeof(DateTimeOffset)
            || typeToConvert == typeof(DateTimeOffset?);

        public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
        {
            if (typeToConvert == typeof(ZonedDateTime)) return new ZonedDateTimeInner();
            if (typeToConvert == typeof(DateTimeOffset)) return new DateTimeOffsetInner();
            if (typeToConvert == typeof(DateTimeOffset?)) return new NullableDateTimeOffsetInner();
            throw new NotSupportedException(typeToConvert.FullName);
        }

        internal static DateTimeOffset ParseDateTimeOffset(string value)
        {
            var normalized = value?.Trim();
            var match = MemgraphZonedDateTimePattern.Match(normalized);
            if (match.Success)
                normalized = match.Groups[1].Value;
            var zoneStart = normalized?.IndexOf('[') ?? -1;
            if (zoneStart >= 0)
                normalized = normalized.Substring(0, zoneStart);
            if (DateTimeOffset.TryParse(normalized, null, System.Globalization.DateTimeStyles.RoundtripKind, out var result))
                return result;
            if (DateTime.TryParse(normalized, null, System.Globalization.DateTimeStyles.AssumeUniversal, out var dt))
                return new DateTimeOffset(dt, TimeSpan.Zero);
            throw new FormatException($"Cannot parse '{value}' as DateTimeOffset. Normalized value: '{normalized}'. Expected ISO 8601 or Memgraph ZonedDateTime format.");
        }

        // Reads a DateTimeOffset from either a string or a Memgraph object
        // e.g. {"Year":2020,"Month":2,"Day":13,"Hour":19,"Minute":49,"Second":54,"Nanosecond":0,"UtcOffsetMinutes":0}
        internal static DateTimeOffset ReadDateTimeOffset(ref Utf8JsonReader reader)
        {
            if (reader.TokenType == JsonTokenType.String)
                return ParseDateTimeOffset(reader.GetString());

            if (reader.TokenType == JsonTokenType.StartObject)
            {
                int year = 0, month = 1, day = 1, hour = 0, minute = 0, second = 0, offsetMinutes = 0;
                while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
                {
                    if (reader.TokenType != JsonTokenType.PropertyName) continue;
                    var propName = reader.GetString();
                    reader.Read();
                    switch (propName)
                    {
                        case "Year":             year          = reader.GetInt32(); break;
                        case "Month":            month         = reader.GetInt32(); break;
                        case "Day":              day           = reader.GetInt32(); break;
                        case "Hour":             hour          = reader.GetInt32(); break;
                        case "Minute":           minute        = reader.GetInt32(); break;
                        case "Second":           second        = reader.GetInt32(); break;
                        case "UtcOffsetMinutes": offsetMinutes = reader.GetInt32(); break;
                    }
                }
                var offset = TimeSpan.FromMinutes(offsetMinutes);
                return new DateTimeOffset(year, month, day, hour, minute, second, offset);
            }

            throw new JsonException($"Cannot read DateTimeOffset from token {reader.TokenType}");
        }

        private class ZonedDateTimeInner : JsonConverter<ZonedDateTime>
        {
            public override ZonedDateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
                => new ZonedDateTime(ReadDateTimeOffset(ref reader));

            public override void Write(Utf8JsonWriter writer, ZonedDateTime value, JsonSerializerOptions options)
                => writer.WriteStringValue(value.As<DateTimeOffset>().ToString("o"));
        }

        private class DateTimeOffsetInner : JsonConverter<DateTimeOffset>
        {
            public override DateTimeOffset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
                => ReadDateTimeOffset(ref reader);

            public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options)
                => writer.WriteStringValue(value.ToString("o"));
        }

        private class NullableDateTimeOffsetInner : JsonConverter<DateTimeOffset?>
        {
            public override DateTimeOffset? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                if (reader.TokenType == JsonTokenType.Null) return null;
                return ReadDateTimeOffset(ref reader);
            }

            public override void Write(Utf8JsonWriter writer, DateTimeOffset? value, JsonSerializerOptions options)
            {
                if (value == null) writer.WriteNullValue();
                else writer.WriteStringValue(value.Value.ToString("o"));
            }
        }
    }


    public class LocalDateTimeConverter : JsonConverterFactory
    {
        // Matches Memgraph format: LocalDateTime('2020-02-13T19:49:54')
        private static readonly Regex MemgraphLocalDateTimePattern =
            new Regex(@"^LocalDateTime\('(.+?)'\)$", RegexOptions.Compiled);

        public override bool CanConvert(Type typeToConvert)
            => typeToConvert == typeof(LocalDateTime)
            || typeToConvert == typeof(DateTime)
            || typeToConvert == typeof(DateTime?);

        public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
        {
            if (typeToConvert == typeof(LocalDateTime)) return new LocalDateTimeInner();
            if (typeToConvert == typeof(DateTime)) return new DateTimeInner();
            if (typeToConvert == typeof(DateTime?)) return new NullableDateTimeInner();
            throw new NotSupportedException(typeToConvert.FullName);
        }

        internal static DateTime ParseDateTime(string value)
        {
            var match = MemgraphLocalDateTimePattern.Match(value);
            if (match.Success)
                value = match.Groups[1].Value;
            return DateTime.Parse(value, null, System.Globalization.DateTimeStyles.RoundtripKind);
        }

        // Reads a DateTime from either a string or a Memgraph object
        // e.g. {"Year":2020,"Month":2,"Day":13,"Hour":19,"Minute":49,"Second":54,"Nanosecond":0}
        internal static DateTime ReadDateTime(ref Utf8JsonReader reader)
        {
            if (reader.TokenType == JsonTokenType.String)
                return ParseDateTime(reader.GetString());

            if (reader.TokenType == JsonTokenType.StartObject)
            {
                int year = 0, month = 1, day = 1, hour = 0, minute = 0, second = 0;
                while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
                {
                    if (reader.TokenType != JsonTokenType.PropertyName) continue;
                    var propName = reader.GetString();
                    reader.Read();
                    switch (propName)
                    {
                        case "Year":   year   = reader.GetInt32(); break;
                        case "Month":  month  = reader.GetInt32(); break;
                        case "Day":    day    = reader.GetInt32(); break;
                        case "Hour":   hour   = reader.GetInt32(); break;
                        case "Minute": minute = reader.GetInt32(); break;
                        case "Second": second = reader.GetInt32(); break;
                    }
                }
                return new DateTime(year, month, day, hour, minute, second);
            }

            throw new JsonException($"Cannot read DateTime from token {reader.TokenType}");
        }

        private class LocalDateTimeInner : JsonConverter<LocalDateTime>
        {
            public override LocalDateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
                => new LocalDateTime(ReadDateTime(ref reader));

            public override void Write(Utf8JsonWriter writer, LocalDateTime value, JsonSerializerOptions options)
                => writer.WriteStringValue(value.As<DateTime>().ToString("o"));
        }

        private class DateTimeInner : JsonConverter<DateTime>
        {
            public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
                => ReadDateTime(ref reader);

            public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
                => writer.WriteStringValue(value.ToString("o"));
        }

        private class NullableDateTimeInner : JsonConverter<DateTime?>
        {
            public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                if (reader.TokenType == JsonTokenType.Null) return null;
                return ReadDateTime(ref reader);
            }

            public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
            {
                if (value == null) writer.WriteNullValue();
                else writer.WriteStringValue(value.Value.ToString("o"));
            }
        }
    }

#if NET6_0_OR_GREATER
    public class DateOnlyConverter : JsonConverterFactory
    {
        // Matches Memgraph format: Date('2024-01-15') or LocalDate('2024-01-15')
        private static readonly Regex MemgraphLocalDatePattern =
            new Regex(@"^(?:LocalDate|Date)\('(.+?)'\)$", RegexOptions.Compiled);

        public override bool CanConvert(Type typeToConvert)
            => typeToConvert == typeof(LocalDate)
            || typeToConvert == typeof(DateOnly)
            || typeToConvert == typeof(DateOnly?);

        public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
        {
            if (typeToConvert == typeof(LocalDate)) return new LocalDateInner();
            if (typeToConvert == typeof(DateOnly)) return new DateOnlyInner();
            if (typeToConvert == typeof(DateOnly?)) return new NullableDateOnlyInner();
            throw new NotSupportedException(typeToConvert.FullName);
        }

        internal static DateOnly ParseDateOnly(string value)
        {
            var normalized = value?.Trim();
            var match = new Regex(@"^(?:LocalDate|Date)\('(.+?)'\)$").Match(normalized);
            if (match.Success)
                normalized = match.Groups[1].Value;
            return DateOnly.Parse(normalized);
        }

        // Reads a DateOnly from either a string ("2024-01-15") or an object ({"Year":1994,"Month":2,"Day":26})
        internal static DateOnly ReadDateOnly(ref Utf8JsonReader reader)
        {
            if (reader.TokenType == JsonTokenType.String)
                return ParseDateOnly(reader.GetString());

            if (reader.TokenType == JsonTokenType.StartObject)
            {
                int year = 0, month = 1, day = 1;
                while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
                {
                    if (reader.TokenType != JsonTokenType.PropertyName) continue;
                    var propName = reader.GetString();
                    reader.Read();
                    switch (propName)
                    {
                        case "Year":  year  = reader.GetInt32(); break;
                        case "Month": month = reader.GetInt32(); break;
                        case "Day":   day   = reader.GetInt32(); break;
                    }
                }
                return new DateOnly(year, month, day);
            }

            throw new JsonException($"Cannot read DateOnly from token {reader.TokenType}");
        }

        private class LocalDateInner : JsonConverter<LocalDate>
        {
            public override LocalDate Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                var d = ReadDateOnly(ref reader);
                return new LocalDate(d.Year, d.Month, d.Day);
            }

            public override void Write(Utf8JsonWriter writer, LocalDate value, JsonSerializerOptions options)
                => writer.WriteStringValue(new DateOnly(value.Year, value.Month, value.Day).ToString("yyyy-MM-dd"));
        }

        private class DateOnlyInner : JsonConverter<DateOnly>
        {
            public override DateOnly Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
                => ReadDateOnly(ref reader);

            public override void Write(Utf8JsonWriter writer, DateOnly value, JsonSerializerOptions options)
                => writer.WriteStringValue(value.ToString("yyyy-MM-dd"));
        }

        private class NullableDateOnlyInner : JsonConverter<DateOnly?>
        {
            public override DateOnly? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                if (reader.TokenType == JsonTokenType.Null) return null;
                return ReadDateOnly(ref reader);
            }

            public override void Write(Utf8JsonWriter writer, DateOnly? value, JsonSerializerOptions options)
            {
                if (value == null) writer.WriteNullValue();
                else writer.WriteStringValue(value.Value.ToString("yyyy-MM-dd"));
            }
        }
    }
#endif
}
