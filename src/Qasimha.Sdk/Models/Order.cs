using System.Text.Json.Serialization;
using Qasimha.Sdk.Json;

namespace Qasimha.Sdk;

/// <summary>
/// An order in full, from <see cref="OrdersResource.GetAsync"/>. Fields the platform keeps internal are
/// not typed here; everything the API returned stays reachable through <see cref="ApiResource.Raw"/>.
/// </summary>
public sealed class Order : ApiResource
{
    private IReadOnlyList<OrderItem> _items = Array.Empty<OrderItem>();
    private IReadOnlyList<OrderInstallment> _installments = Array.Empty<OrderInstallment>();
    private IReadOnlyList<OrderFulfillment> _fulfillments = Array.Empty<OrderFulfillment>();
    private IReadOnlyList<OrderReversal> _reversals = Array.Empty<OrderReversal>();
    private IReadOnlyList<OrderVoid> _voids = Array.Empty<OrderVoid>();
    private IReadOnlyList<OrderStatusChange> _statusHistory = Array.Empty<OrderStatusChange>();

    /// <summary>The Qasimha order id.</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>Your order reference.</summary>
    public string? OrderReferenceId { get; init; }

    /// <summary>The order status, as an <see cref="OrderStatus"/> name.</summary>
    [JsonConverter(typeof(OrderStatusConverter))]
    public string? Status { get; init; }

    /// <summary>The payment type, as a <see cref="PaymentType"/> name.</summary>
    [JsonConverter(typeof(PaymentTypeConverter))]
    public string? PaymentType { get; init; }

    /// <summary>The channel, as an <see cref="OrderChannel"/> name.</summary>
    [JsonConverter(typeof(OrderChannelConverter))]
    public string? Channel { get; init; }

    /// <summary>How the hosted checkout was reached, when recorded.</summary>
    [JsonConverter(typeof(LenientStringConverter))]
    public string? CheckoutChannel { get; init; }

    /// <summary>The financed amount.</summary>
    public decimal TotalAmount { get; init; }

    /// <summary>Order-level tax.</summary>
    public decimal TaxAmount { get; init; }

    /// <summary>Shipping charged.</summary>
    public decimal ShippingAmount { get; init; }

    /// <summary>Order-level discount.</summary>
    public decimal DiscountAmount { get; init; }

    /// <summary>ISO 4217 currency code.</summary>
    public string? CurrencyCode { get; init; }

    /// <summary>ISO 3166-1 alpha-2 country code.</summary>
    public string? CountryCode { get; init; }

    /// <summary>The description you sent.</summary>
    public string? Description { get; init; }

    /// <summary>Number of instalments.</summary>
    public int? Instalments { get; init; }

    /// <summary>Down payment taken at checkout.</summary>
    public decimal? DownPaymentAmount { get; init; }

    /// <summary>Amount approved by the decision, when approved.</summary>
    public decimal? ApprovedAmount { get; init; }

    /// <summary>Amount captured so far.</summary>
    public decimal CapturedAmount { get; init; }

    /// <summary>Amount fulfilled so far.</summary>
    public decimal FulfilledAmount { get; init; }

    /// <summary>Amount refunded so far.</summary>
    public decimal RefundedAmount { get; init; }

    /// <summary>What can still be refunded: the customer's paid instalments minus refunds so far.</summary>
    public decimal RefundableAmount { get; init; }

    /// <summary>Amount the customer has paid.</summary>
    public decimal CollectedAmount { get; init; }

    /// <summary>Amount voided.</summary>
    public decimal VoidedAmount { get; init; }

    /// <summary>Amount reversed.</summary>
    public decimal ReversedAmount { get; init; }

    /// <summary>The hosted checkout URL, while the checkout is open.</summary>
    public string? PaymentUrl { get; init; }

    /// <summary>The checkout provider's session id.</summary>
    public string? ProviderSessionId { get; init; }

    /// <summary>The checkout provider's order id.</summary>
    public string? ProviderOrderId { get; init; }

    /// <summary>When the order was created.</summary>
    public DateTimeOffset? CreatedAt { get; init; }

    /// <summary>When the order was confirmed.</summary>
    public DateTimeOffset? ConfirmedAt { get; init; }

    /// <summary>When the order was fully fulfilled.</summary>
    public DateTimeOffset? FulfilledAt { get; init; }

    /// <summary>When the order was voided.</summary>
    public DateTimeOffset? VoidedAt { get; init; }

    /// <summary>When the order was reversed.</summary>
    public DateTimeOffset? ReversedAt { get; init; }

    /// <summary>When the order was settled to you.</summary>
    public DateTimeOffset? SettledAt { get; init; }

    /// <summary>When the hosted checkout expires.</summary>
    public DateTimeOffset? CheckoutExpiresAt { get; init; }

    /// <summary>When the order expires.</summary>
    public DateTimeOffset? ExpiresAt { get; init; }

    /// <summary>The Ejar contract number, for real-estate merchants.</summary>
    public string? EjarContractNumber { get; init; }

    /// <summary>The language the customer is notified in.</summary>
    public string? NotificationLanguage { get; init; }

    /// <summary>The financing contract number, once accepted.</summary>
    public string? ContractNumber { get; init; }

    /// <summary>The customer.</summary>
    public OrderCustomer? Customer { get; init; }

    /// <summary>The basket lines, as sent.</summary>
    public IReadOnlyList<OrderItem> Items
    {
        get => _items;
        init => _items = value ?? Array.Empty<OrderItem>();
    }

    /// <summary>The instalment schedule.</summary>
    public IReadOnlyList<OrderInstallment> Installments
    {
        get => _installments;
        init => _installments = value ?? Array.Empty<OrderInstallment>();
    }

    /// <summary>Fulfilments recorded.</summary>
    public IReadOnlyList<OrderFulfillment> Fulfillments
    {
        get => _fulfillments;
        init => _fulfillments = value ?? Array.Empty<OrderFulfillment>();
    }

    /// <summary>Refunds (reversals) recorded.</summary>
    public IReadOnlyList<OrderReversal> Reversals
    {
        get => _reversals;
        init => _reversals = value ?? Array.Empty<OrderReversal>();
    }

    /// <summary>Voids recorded.</summary>
    public IReadOnlyList<OrderVoid> Voids
    {
        get => _voids;
        init => _voids = value ?? Array.Empty<OrderVoid>();
    }

    /// <summary>The status history.</summary>
    public IReadOnlyList<OrderStatusChange> StatusHistory
    {
        get => _statusHistory;
        init => _statusHistory = value ?? Array.Empty<OrderStatusChange>();
    }
}
