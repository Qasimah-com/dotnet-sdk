using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Qasimha.Sdk.Json;

internal abstract class EnumNameConverter : JsonConverter<string?>
{
    protected abstract IReadOnlyList<string> Names { get; }

    public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.Number:
                return reader.TryGetInt64(out var code)
                    ? Normalize(code)
                    : reader.GetDecimal().ToString(CultureInfo.InvariantCulture);
            case JsonTokenType.String:
                return Normalize(reader.GetString() ?? string.Empty);
            case JsonTokenType.True:
            case JsonTokenType.False:
                return reader.GetBoolean() ? "true" : "false";
            default:
                throw new JsonException(FormattableString.Invariant($"Unexpected token {reader.TokenType} for an enumerated value."));
        }
    }

    public override void Write(Utf8JsonWriter writer, string? value, JsonSerializerOptions options)
    {
        if (value is null)
        {
            writer.WriteNullValue();
        }
        else
        {
            writer.WriteStringValue(value);
        }
    }

    public string Normalize(long code) =>
        code >= 0 && code < Names.Count ? Names[(int)code] : code.ToString(CultureInfo.InvariantCulture);

    public string Normalize(string value)
    {
        if (long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var code))
        {
            return Normalize(code);
        }

        foreach (var name in Names)
        {
            if (string.Equals(name, value, StringComparison.OrdinalIgnoreCase))
            {
                return name;
            }
        }

        return value;
    }
}

internal sealed class OrderStatusConverter : EnumNameConverter
{
    protected override IReadOnlyList<string> Names => OrderStatus.Names;
}

internal sealed class OrderChannelConverter : EnumNameConverter
{
    protected override IReadOnlyList<string> Names => OrderChannel.Names;
}

internal sealed class PaymentTypeConverter : EnumNameConverter
{
    protected override IReadOnlyList<string> Names => PaymentType.Names;
}

internal sealed class InstallmentItemStatusConverter : EnumNameConverter
{
    protected override IReadOnlyList<string> Names => InstallmentItemStatus.Names;
}
