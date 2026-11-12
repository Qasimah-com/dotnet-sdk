namespace Qasimha.Sdk;

/// <summary>
/// Aliases accepted by <see cref="ListOrdersRequest.Status"/> besides the <see cref="OrderStatus"/>
/// names. They match the merchant portal's tabs and <see cref="OrderCounts"/>.
/// </summary>
public static class OrderStatusFilter
{
    /// <summary>Approved orders.</summary>
    public const string Approved = "approved";

    /// <summary>Authorised (confirmed, not yet captured) orders.</summary>
    public const string Authorised = "authorised";

    /// <summary>Captured (fulfilled) orders.</summary>
    public const string Captured = "captured";

    /// <summary>Refunded (reversed) orders.</summary>
    public const string Refunded = "refunded";

    /// <summary>Cancelled (voided) orders.</summary>
    public const string Cancelled = "cancelled";
}
