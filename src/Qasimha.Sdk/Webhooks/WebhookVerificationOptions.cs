namespace Qasimha.Sdk;

/// <summary>Per-call settings for webhook verification.</summary>
public sealed class WebhookVerificationOptions
{
    /// <summary>How far the delivery's timestamp may be from <see cref="Now"/>. Default 300 seconds.</summary>
    public TimeSpan Tolerance { get; set; } = WebhookVerifier.DefaultTolerance;

    /// <summary>The current time; defaults to <see cref="DateTimeOffset.UtcNow"/>. Set it in tests.</summary>
    public DateTimeOffset? Now { get; set; }
}
