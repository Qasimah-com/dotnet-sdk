namespace Qasimha.Sdk;

/// <summary>
/// The result of <see cref="WebhookEndpointsResource.CreateAsync"/> and
/// <see cref="WebhookEndpointsResource.RotateSecretAsync"/>: the endpoint plus its signing secret, returned
/// once. Store <see cref="Secret"/> in a secrets manager; it cannot be read back, is never logged by the SDK
/// and is not part of <see cref="WebhookEndpoint.ToString"/>.
/// </summary>
public sealed class WebhookEndpointWithSecret : WebhookEndpoint
{
    /// <summary>The signing secret (<c>whsec_…</c>) for <see cref="QasimhaClientOptions.WebhookSecret"/>.</summary>
    public string? Secret { get; init; }
}
