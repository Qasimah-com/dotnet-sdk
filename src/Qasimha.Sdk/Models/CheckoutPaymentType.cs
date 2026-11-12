namespace Qasimha.Sdk;

/// <summary>The <c>paymentType</c> values accepted by <see cref="CreateSessionRequest"/>. Any other value is refused before sending.</summary>
public static class CheckoutPaymentType
{
    /// <summary>The default: split into instalments.</summary>
    public const string PayByInstalments = "pay_by_instalments";

    /// <summary>Pay now.</summary>
    public const string PayNow = "pay_now";

    /// <summary>Pay next month.</summary>
    public const string PayNextMonth = "pay_next_month";

    /// <summary>Pay in full.</summary>
    public const string PayInFull = "pay_in_full";

    internal static readonly string[] Known = { PayByInstalments, PayNow, PayNextMonth, PayInFull };

    internal static bool IsKnown(string? value)
    {
        foreach (var known in Known)
        {
            if (string.Equals(known, value, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }
}
