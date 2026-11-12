using System.Text.Json.Serialization;
using Qasimha.Sdk.Json;

namespace Qasimha.Sdk;

/// <summary>One event's delivery to one endpoint, with every attempt's outcome.</summary>
public sealed class WebhookDelivery : ApiResource
{
    /// <summary>The delivery id.</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>The event delivered. Deliveries report it as a bare GUID; the <c>X-Qasimha-Event-Id</c> header carries the <c>evt_</c> form.</summary>
    public string? EventId { get; init; }

    /// <summary>The <see cref="WebhookEventType"/> name.</summary>
    public string? EventType { get; init; }

    /// <summary>The Qasimha order id, when the event concerns an order.</summary>
    public string? OrderId { get; init; }

    /// <summary>Your order reference, when the event concerns an order.</summary>
    public string? OrderReferenceId { get; init; }

    /// <summary>A <see cref="WebhookDeliveryStatus"/> name.</summary>
    [JsonConverter(typeof(LenientStringConverter))]
    public string? Status { get; init; }

    /// <summary>Attempts made so far.</summary>
    public int Attempts { get; init; }

    /// <summary>Attempts allowed before the delivery ends as failed.</summary>
    public int MaxAttempts { get; init; }

    /// <summary>When the next retry is due, while pending.</summary>
    public DateTimeOffset? NextAttemptAt { get; init; }

    /// <summary>When the last attempt was made.</summary>
    public DateTimeOffset? LastAttemptAt { get; init; }

    /// <summary>The HTTP status your server answered on the last attempt.</summary>
    public int? LastStatusCode { get; init; }

    /// <summary>What went wrong on the last attempt.</summary>
    public string? LastError { get; init; }

    /// <summary>The first characters of your server's response on the last attempt.</summary>
    public string? ResponseSnippet { get; init; }

    /// <summary>How long the last attempt took.</summary>
    public int? DurationMs { get; init; }

    /// <summary>When your server accepted the delivery.</summary>
    public DateTimeOffset? SucceededAt { get; init; }

    /// <summary>The delivery this one replays, when created by <see cref="WebhookEndpointsResource.ReplayDeliveryAsync"/>.</summary>
    public string? ReplayOfId { get; init; }

    /// <summary>When the delivery was created.</summary>
    public DateTimeOffset? CreatedAt { get; init; }
}
