using System.Text.Json.Serialization;
using Qasimha.Sdk.Json;

namespace Qasimha.Sdk;

/// <summary>
/// A payment-link or QR order. Create, get and list return different subsets of these members today,
/// so every member except <see cref="Id"/> is optional; the full payload is in <see cref="ApiResource.Raw"/>.
/// </summary>
public sealed class CheckoutLink : ApiResource
{
    private IReadOnlyList<CheckoutLinkLine> _items = Array.Empty<CheckoutLinkLine>();

    /// <summary>The Qasimha order id.</summary>
    [JsonInclude]
    public string Id { get; internal set; } = string.Empty;

    /// <summary>The hosted checkout page, where the API returned it.</summary>
    [JsonInclude]
    public string? Url { get; internal set; }

    /// <summary><see cref="CheckoutLinkChannel.Link"/> or <see cref="CheckoutLinkChannel.Qr"/>.</summary>
    [JsonConverter(typeof(LenientStringConverter))]
    public string? Channel { get; init; }

    /// <summary>The amount to finance.</summary>
    public decimal? Amount { get; init; }

    /// <summary>ISO 4217 currency code.</summary>
    [JsonInclude]
    public string? Currency { get; internal set; }

    /// <summary>The customer's phone; masked on reads.</summary>
    public string? Phone { get; init; }

    /// <summary>The notification language.</summary>
    public string? Language { get; init; }

    /// <summary>Your order reference.</summary>
    public string? OrderReferenceId { get; init; }

    /// <summary>The Ejar contract number, for real-estate merchants.</summary>
    public string? EjarContractNumber { get; init; }

    /// <summary>The basket lines, when returned.</summary>
    public IReadOnlyList<CheckoutLinkLine> Items
    {
        get => _items;
        init => _items = value ?? Array.Empty<CheckoutLinkLine>();
    }

    /// <summary>The order status, as an <see cref="OrderStatus"/> name; <see cref="OrderStatus.AwaitingCheckout"/> on creation.</summary>
    [JsonConverter(typeof(OrderStatusConverter))]
    public string? Status { get; init; }

    /// <summary>The credit decision, when one has been made.</summary>
    [JsonConverter(typeof(LenientStringConverter))]
    public string? DecisionStatus { get; init; }

    /// <summary>When the link stops being usable (two hours after creation).</summary>
    public DateTimeOffset? ExpiresAt { get; init; }

    /// <summary>When the link was created.</summary>
    public DateTimeOffset? CreatedAt { get; init; }

    /// <summary><see langword="true"/> once the customer finished checkout.</summary>
    public bool? Completed { get; init; }

    /// <summary><see langword="true"/> when the credit decision rejected it.</summary>
    public bool? Declined { get; init; }

    /// <summary><see langword="true"/> while the link is still open for checkout (list rows).</summary>
    public bool? Waiting { get; init; }

    internal override void AfterMaterialize()
    {
        if (Id.Length == 0)
        {
            Id = Raw.GetStringOrNull("orderId") ?? Raw.GetStringOrNull("installmentId") ?? string.Empty;
        }

        Url ??= Raw.GetStringOrNull("paymentUrl") ?? Raw.GetStringOrNull("checkoutUrl");
        Currency ??= Raw.GetStringOrNull("currencyCode");
    }
}
