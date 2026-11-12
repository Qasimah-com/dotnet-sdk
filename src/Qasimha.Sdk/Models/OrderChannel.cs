using Qasimha.Sdk.Json;

namespace Qasimha.Sdk;

/// <summary>Order channel names. The API may send the integer code or the name; the SDK always exposes the name.</summary>
public static class OrderChannel
{
    /// <summary>Code 0: a payment session created by your server.</summary>
    public const string Online = "Online";

    /// <summary>Code 1: a POS order.</summary>
    public const string InStore = "InStore";

    /// <summary>Code 2: a QR or payment-link order.</summary>
    public const string QrCode = "QrCode";

    internal static readonly string[] Names = { Online, InStore, QrCode };

    private static readonly OrderChannelConverter Converter = new();

    /// <summary>Maps an integer code to its name; an unknown code comes back as its decimal string.</summary>
    /// <param name="code">The integer code.</param>
    public static string FromCode(int code) => Converter.Normalize(code);

    /// <summary>Maps a name or a numeric string to the canonical name; an unknown value is returned unchanged.</summary>
    /// <param name="value">The name or numeric string.</param>
    public static string Normalize(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return Converter.Normalize(value);
    }
}
