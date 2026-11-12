using Qasimha.Sdk.Json;

namespace Qasimha.Sdk;

/// <summary>
/// Order status names. The API may send the integer code or the name; the SDK always exposes the
/// name, and passes an unknown value through as the string it received.
/// </summary>
public static class OrderStatus
{
    /// <summary>Code 0.</summary>
    public const string Draft = "Draft";

    /// <summary>Code 1.</summary>
    public const string New = "New";

    /// <summary>Code 2.</summary>
    public const string Approved = "Approved";

    /// <summary>Code 3.</summary>
    public const string Declined = "Declined";

    /// <summary>Code 4.</summary>
    public const string Expired = "Expired";

    /// <summary>Code 5: the hosted checkout finished and the first instalment was paid.</summary>
    public const string Confirmed = "Confirmed";

    /// <summary>Code 6.</summary>
    public const string FullyFulfilled = "FullyFulfilled";

    /// <summary>Code 7.</summary>
    public const string PartiallyFulfilled = "PartiallyFulfilled";

    /// <summary>Code 8.</summary>
    public const string FullyReversed = "FullyReversed";

    /// <summary>Code 9.</summary>
    public const string PartiallyReversed = "PartiallyReversed";

    /// <summary>Code 10.</summary>
    public const string Voided = "Voided";

    /// <summary>Code 11.</summary>
    public const string Updated = "Updated";

    /// <summary>Code 12: created, waiting for the customer to open the hosted checkout.</summary>
    public const string AwaitingCheckout = "AwaitingCheckout";

    /// <summary>Code 13.</summary>
    public const string Cancelled = "Cancelled";

    /// <summary>Code 14.</summary>
    public const string Settled = "Settled";

    /// <summary>Code 15.</summary>
    public const string WrittenOff = "WrittenOff";

    internal static readonly string[] Names =
    {
        Draft, New, Approved, Declined, Expired, Confirmed, FullyFulfilled, PartiallyFulfilled,
        FullyReversed, PartiallyReversed, Voided, Updated, AwaitingCheckout, Cancelled, Settled, WrittenOff,
    };

    private static readonly OrderStatusConverter Converter = new();

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
