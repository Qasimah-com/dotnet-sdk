namespace Qasimha.Sdk;

/// <summary>
/// A webhook endpoint registered for your account. The signing secret is never returned here:
/// <see cref="SecretHint"/> identifies which secret a server holds.
/// </summary>
public class WebhookEndpoint : ApiResource
{
    private IReadOnlyList<string> _events = Array.Empty<string>();

    /// <summary>The endpoint id.</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>The HTTPS URL deliveries are posted to.</summary>
    public string? Url { get; init; }

    /// <summary>Your description of the endpoint.</summary>
    public string? Description { get; init; }

    /// <summary>The subscribed <see cref="WebhookEventType"/> names; empty means every event.</summary>
    public IReadOnlyList<string> Events
    {
        get => _events;
        init => _events = value ?? Array.Empty<string>();
    }

    /// <summary><see langword="false"/> while deliveries are paused.</summary>
    public bool IsActive { get; init; }

    /// <summary>The last characters of the live secret.</summary>
    public string? SecretHint { get; init; }

    /// <summary>Until when the previous secret still signs deliveries after a rotation; <see langword="null"/> when none is pending.</summary>
    public DateTimeOffset? PreviousSecretExpiresAt { get; init; }

    /// <summary>When the endpoint was registered.</summary>
    public DateTimeOffset? CreatedAt { get; init; }

    /// <summary>When the endpoint was last changed.</summary>
    public DateTimeOffset? UpdatedAt { get; init; }

    /// <summary>The id and the secret hint. Never the secret.</summary>
    public override string ToString() => $"WebhookEndpoint({Id}, …{SecretHint})";
}
