namespace Qasimha.Sdk;

/// <summary>Input for <see cref="OrdersResource.RefundAsync"/>.</summary>
public sealed class RefundRequest
{
    /// <summary>The amount to refund, at most the order's refundable balance.</summary>
    public required decimal Amount { get; set; }

    /// <summary>Why, for your records and the customer's.</summary>
    public string? Reason { get; set; }
}
