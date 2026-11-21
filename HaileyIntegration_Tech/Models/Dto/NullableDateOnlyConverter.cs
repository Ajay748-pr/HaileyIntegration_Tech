using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace HaileyIntegration.Tech.Models.Dto;

/// <summary>
/// Reads optional dates from Hailey without throwing: null, empty string or an
/// unparseable value becomes null. Accepts "yyyy-MM-dd" as well as full date-time strings.
/// </summary>
public sealed class NullableDateOnlyConverter : JsonConverter<DateOnly?>
{
    public override bool HandleNull => true;

    public override DateOnly? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
        {
            // Null/number/bool are single tokens and need no skipping. Skip() throws on a
            // streamed (non-final) buffer, so use TrySkip for an unexpected object/array;
            // the serializer has already buffered the whole value for custom converters.
            if (reader.TokenType is JsonTokenType.StartObject or JsonTokenType.StartArray)
                reader.TrySkip();
            return null;
        }

        var value = reader.GetString();
        if (string.IsNullOrWhiteSpace(value))
            return null;

        if (DateOnly.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
            return date;

        if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var dateTime))
            return DateOnly.FromDateTime(dateTime);

        return null;
    }

    public override void Write(Utf8JsonWriter writer, DateOnly? value, JsonSerializerOptions options)
    {
        if (value.HasValue)
            writer.WriteStringValue(value.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        else
            writer.WriteNullValue();
    }
}
