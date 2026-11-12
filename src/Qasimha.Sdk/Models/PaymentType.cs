using Qasimha.Sdk.Json;

namespace Qasimha.Sdk;

/// <summary>
/// Payment type names as the API returns them on an order. The request-side vocabulary
/// (<c>pay_by_instalments</c>, …) lives in <see cref="CheckoutPaymentType"/>.
/// </summary>
public static class PaymentType
{
    /// <summary>Code 0.</summary>
    public const string PayByInstalments = "PayByInstalments";

    /// <summary>Code 1.</summary>
    public const string PayNow = "PayNow";

    /// <summary>Code 2.</summary>
    public const string PayNextMonth = "PayNextMonth";

    /// <summary>Code 3.</summary>
    public const string PayInFull = "PayInFull";

    internal static readonly string[] Names = { PayByInstalments, PayNow, PayNextMonth, PayInFull };

    private static readonly PaymentTypeConverter Converter = new();

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
