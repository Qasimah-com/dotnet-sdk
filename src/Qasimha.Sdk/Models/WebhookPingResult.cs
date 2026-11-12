namespace Qasimha.Sdk;

/// <summary>Result of <see cref="WebhookEndpointsResource.PingAsync"/>: a signed <c>ping</c> event was queued.</summary>
public sealed class WebhookPingResult : ApiResource
{
    /// <summary>The queued event's id (<c>evt_…</c>).</summary>
    public string? EventId { get; init; }

    /// <summary>The endpoint it goes to.</summary>
    public string? EndpointId { get; init; }
}
