namespace Qasimha.Sdk;

/// <summary>
/// Event types a webhook endpoint can subscribe to (<see cref="CreateWebhookEndpointRequest.Events"/>)
/// and that arrive as <see cref="WebhookEvent.Type"/>. An endpoint with no subscription receives every event.
/// </summary>
public static class WebhookEventType
{
    /// <summary>The hosted checkout finished and the first instalment was paid.</summary>
    public const string OrderConfirmed = "order.confirmed";

    /// <summary>The credit decision rejected the order.</summary>
    public const string OrderDeclined = "order.declined";

    /// <summary>The order was fully fulfilled.</summary>
    public const string OrderFulfilled = "order.fulfilled";

    /// <summary>Part of the order was fulfilled.</summary>
    public const string OrderPartiallyFulfilled = "order.partially_fulfilled";

    /// <summary>The order was fully refunded.</summary>
    public const string OrderRefunded = "order.refunded";

    /// <summary>Part of the order was refunded.</summary>
    public const string OrderPartiallyRefunded = "order.partially_refunded";

    /// <summary>The order was voided.</summary>
    public const string OrderVoided = "order.voided";

    /// <summary>The order was cancelled.</summary>
    public const string OrderCancelled = "order.cancelled";

    /// <summary>The order expired before checkout completed.</summary>
    public const string OrderExpired = "order.expired";

    /// <summary>A payment link expired unused.</summary>
    public const string CheckoutLinkExpired = "checkout_link.expired";

    /// <summary>Accepted as an alias of <see cref="OrderConfirmed"/> for one API release; subscribe to <see cref="OrderConfirmed"/> instead.</summary>
    public const string OrderCompleted = "order.completed";

    /// <summary>The signed test event sent by <see cref="WebhookEndpointsResource.PingAsync"/>; not a subscription.</summary>
    public const string Ping = "ping";

    internal static readonly string[] Known =
    {
        OrderConfirmed, OrderDeclined, OrderFulfilled, OrderPartiallyFulfilled, OrderRefunded, OrderPartiallyRefunded,
        OrderVoided, OrderCancelled, OrderExpired, CheckoutLinkExpired, OrderCompleted,
    };

    internal static bool IsKnown(string? value)
    {
        foreach (var name in Known)
        {
            if (string.Equals(name, value, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }
}
