using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Qasimha.Sdk.Json;

internal sealed class DecimalOrZeroConverter : JsonConverter<decimal>
{
    public override bool HandleNull => true;

    public override decimal Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.Null:
                return 0m;
            case JsonTokenType.Number:
                return reader.GetDecimal();
            case JsonTokenType.String:
                var text = reader.GetString();
                return decimal.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed)
                    ? parsed
                    : throw new JsonException("The value '" + text + "' is not a decimal number.");
            default:
                throw new JsonException(FormattableString.Invariant($"Unexpected token {reader.TokenType} for a decimal amount."));
        }
    }

    public override void Write(Utf8JsonWriter writer, decimal value, JsonSerializerOptions options) =>
        writer.WriteNumberValue(value);
}
