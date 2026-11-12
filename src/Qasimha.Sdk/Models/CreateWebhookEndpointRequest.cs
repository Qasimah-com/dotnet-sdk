namespace Qasimha.Sdk;

/// <summary>Input for <see cref="WebhookEndpointsResource.CreateAsync"/>. Up to 5 endpoints per account.</summary>
public sealed class CreateWebhookEndpointRequest
{
    /// <summary>The URL deliveries are posted to. Must be <c>https://</c>; the API also refuses private-network targets.</summary>
    public required string Url { get; set; }

    /// <summary>Your description, for the portal and your own records.</summary>
    public string? Description { get; set; }

    /// <summary>The <see cref="WebhookEventType"/> names to receive. Omit or leave empty to receive every event.</summary>
    public IReadOnlyList<string>? Events { get; set; }
}
