namespace Qasimha.Sdk;

/// <summary>Settings for a <see cref="QasimhaClient"/>. Validated once, when the client is constructed.</summary>
public sealed class QasimhaClientOptions
{
    /// <summary>Your merchant API key: <c>sk_test_…</c> for the sandbox or <c>sk_live_…</c> for production. Required.</summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// The environment to talk to. Inferred from the key prefix when omitted; when given it must agree
    /// with the prefix unless <see cref="BaseUrl"/> is also set.
    /// </summary>
    public QasimhaEnvironment? Environment { get; set; }

    /// <summary>
    /// Overrides the environment's base URL. Must be <c>https://</c> unless the host is
    /// <c>localhost</c> or <c>127.0.0.1</c>.
    /// </summary>
    public string? BaseUrl { get; set; }

    /// <summary>Timeout per attempt (connect and read). Default 30 seconds.</summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>How many times a retryable failure is retried, between 0 and 5. Default 2.</summary>
    public int MaxRetries { get; set; } = 2;

    /// <summary>The webhook signing secret (<c>whsec_…</c>) used by <see cref="QasimhaClient.Webhooks"/> when no secret is passed per call.</summary>
    public string? WebhookSecret { get; set; }

    /// <summary>
    /// Your publishable key (<c>pk_test_…</c> / <c>pk_live_…</c>), used by
    /// <see cref="CheckoutResource.GetPlansAsync"/> when no key is passed per call. Optional; its
    /// environment must agree with <see cref="ApiKey"/>'s.
    /// </summary>
    public string? PublishableKey { get; set; }

    /// <summary>Language of API messages, sent as <c>Accept-Language</c>: <c>"en"</c> (default) or <c>"ar"</c>.</summary>
    public string Language { get; set; } = "en";

    /// <summary>Appended to the <c>User-Agent</c> header, for example <c>"woocommerce/1.2.0"</c>.</summary>
    public string? UserAgentSuffix { get; set; }

    /// <summary>Receives the SDK's log entries. No logging when unset.</summary>
    public QasimhaLogger? Logger { get; set; }

    /// <summary>Waits between retries. Defaults to <see cref="TaskDelay.Instance"/>; tests replace it to run instantly.</summary>
    public IDelay? Delay { get; set; }

    internal Func<DateTimeOffset>? Clock { get; set; }
}
