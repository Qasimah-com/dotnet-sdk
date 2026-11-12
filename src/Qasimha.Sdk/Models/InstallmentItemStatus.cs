using Qasimha.Sdk.Json;

namespace Qasimha.Sdk;

/// <summary>Status names of one instalment in an order's schedule. The API may send the integer code or the name; the SDK always exposes the name.</summary>
public static class InstallmentItemStatus
{
    /// <summary>Code 0.</summary>
    public const string Scheduled = "Scheduled";

    /// <summary>Code 1.</summary>
    public const string Paid = "Paid";

    /// <summary>Code 2.</summary>
    public const string Late = "Late";

    /// <summary>Code 3.</summary>
    public const string Reversed = "Reversed";

    /// <summary>Code 4.</summary>
    public const string Waived = "Waived";

    internal static readonly string[] Names = { Scheduled, Paid, Late, Reversed, Waived };

    private static readonly InstallmentItemStatusConverter Converter = new();

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
