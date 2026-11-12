namespace Qasimha.Sdk;

/// <summary>Input for <see cref="WebhookEndpointsResource.UpdateAsync"/>. Unset members are not sent and keep their value.</summary>
public sealed class UpdateWebhookEndpointRequest
{
    /// <summary>A new URL. Must be <c>https://</c>.</summary>
    public string? Url { get; set; }

    /// <summary>A new description.</summary>
    public string? Description { get; set; }

    /// <summary>The <see cref="WebhookEventType"/> names to receive. An empty list means every event.</summary>
    public IReadOnlyList<string>? Events { get; set; }

    /// <summary><see langword="false"/> pauses deliveries; <see langword="true"/> resumes them.</summary>
    public bool? IsActive { get; set; }
}
