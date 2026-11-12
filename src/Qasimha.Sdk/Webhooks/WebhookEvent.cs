using System.Globalization;
using System.Text.Json;
using Qasimha.Sdk.Json;

namespace Qasimha.Sdk;

/// <summary>
/// A verified webhook delivery. A registered endpoint (API release of 2026-09-25) receives the versioned
/// body: <c>id</c>, <c>type</c>, <c>createdAt</c>, <c>orderId</c>, <c>orderReferenceId</c>, <c>status</c>,
/// <c>amount</c>, <c>currency</c> and <c>data.order</c>. The global-URL body (<c>eventId</c>, <c>eventType</c>,
/// <c>occurredAt</c>, no order id) maps here too; the whole payload is in <see cref="Raw"/>.
/// </summary>
public sealed class WebhookEvent
{
    internal WebhookEvent()
    {
    }

    /// <summary>The delivery id (<c>evt_…</c>); use it to de-duplicate.</summary>
    public string Id { get; internal init; } = string.Empty;

    /// <summary>The event type, for example <c>order.completed</c>.</summary>
    public string Type { get; internal init; } = string.Empty;

    /// <summary>The Qasimha order id. Deliveries from the operations-configured global URL carry none; use <see cref="OrdersResource.GetByReferenceAsync"/> with <see cref="OrderReferenceId"/> then.</summary>
    public string? OrderId { get; internal init; }

    /// <summary>Your order reference.</summary>
    public string? OrderReferenceId { get; internal init; }

    /// <summary>The order status as sent.</summary>
    public string? Status { get; internal init; }

    /// <summary>The amount the event concerns; for <c>order.reversed</c>, that refund.</summary>
    public decimal? Amount { get; internal init; }

    /// <summary>ISO 4217 currency code.</summary>
    public string? Currency { get; internal init; }

    /// <summary>When the event happened.</summary>
    public DateTimeOffset? OccurredAt { get; internal init; }

    /// <summary>The 1-based delivery attempt from <c>X-Qasimha-Delivery-Attempt</c>, when the header was sent.</summary>
    public int? DeliveryAttempt { get; internal init; }

    /// <summary>The whole decoded body.</summary>
    public JsonElement Raw { get; internal init; }

    internal static WebhookEvent Parse(byte[] body, Func<string, string?>? headers)
    {
        JsonElement root;
        try
        {
            using var document = JsonDocument.Parse(body);
            root = document.RootElement.Clone();
        }
        catch (JsonException ex)
        {
            throw new ValidationException(
                "The webhook body is not valid JSON.",
                new ApiErrorDetails(null, null, null, new[] { new ApiFieldError("body", ex.Message) }, null),
                ex);
        }

        if (root.ValueKind != JsonValueKind.Object)
        {
            throw new ValidationException(
                "The webhook body must be a JSON object.",
                new[] { new ApiFieldError("body", "must be a JSON object") });
        }

        var orderId = root.GetStringOrNull("orderId");
        if (orderId is null
            && root.TryGetPropertyIgnoreCase("data", out var data)
            && data.TryGetPropertyIgnoreCase("order", out var order))
        {
            orderId = order.GetStringOrNull("id");
        }

        string? status = null;
        if (root.TryGetPropertyIgnoreCase("status", out var statusElement))
        {
            status = statusElement.ValueKind == JsonValueKind.Number && statusElement.TryGetInt32(out var code)
                ? OrderStatus.FromCode(code)
                : statusElement.AsStringOrNull();
        }

        DateTimeOffset? occurredAt = null;
        if (root.TryGetPropertyIgnoreCase("occurredAt", out var occurred))
        {
            occurredAt = occurred.AsDateTimeOffsetOrNull();
        }

        if (occurredAt is null && root.TryGetPropertyIgnoreCase("createdAt", out var created))
        {
            occurredAt = created.AsDateTimeOffsetOrNull();
        }

        int? attempt = null;
        var attemptHeader = headers?.Invoke(WebhookVerifier.DeliveryAttemptHeader);
        if (attemptHeader is not null && int.TryParse(attemptHeader, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedAttempt))
        {
            attempt = parsedAttempt;
        }

        return new WebhookEvent
        {
            Id = root.GetStringOrNull("eventId") ?? root.GetStringOrNull("id") ?? headers?.Invoke(WebhookVerifier.EventIdHeader) ?? string.Empty,
            Type = root.GetStringOrNull("eventType") ?? root.GetStringOrNull("type") ?? headers?.Invoke(WebhookVerifier.EventTypeHeader) ?? string.Empty,
            OrderId = orderId,
            OrderReferenceId = root.GetStringOrNull("orderReferenceId"),
            Status = status,
            Amount = root.TryGetPropertyIgnoreCase("amount", out var amount) ? amount.AsDecimalOrNull() : null,
            Currency = root.GetStringOrNull("currency") ?? root.GetStringOrNull("currencyCode"),
            OccurredAt = occurredAt,
            DeliveryAttempt = attempt,
            Raw = root,
        };
    }
}
