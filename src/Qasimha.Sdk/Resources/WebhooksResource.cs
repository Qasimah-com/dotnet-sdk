using System.Text;
using Qasimha.Sdk.Http;

namespace Qasimha.Sdk;

/// <summary>
/// Verifies webhook deliveries and parses them into <see cref="WebhookEvent"/>. Pass the body exactly
/// as received: bytes, or the string read from the raw request stream, never a re-serialised object.
/// Confirm the order with <see cref="OrdersResource.GetAsync"/> before acting on any event.
/// </summary>
public sealed class WebhooksResource
{
    private readonly ClientConfiguration _config;

    internal WebhooksResource(ClientConfiguration config)
    {
        _config = config;
    }

    /// <summary>Verifies a delivery and parses it, reading the headers through <paramref name="headers"/>.</summary>
    /// <param name="rawBody">The raw request body.</param>
    /// <param name="headers">Returns a request header's value by name, or <see langword="null"/>; for example <c>name =&gt; Request.Headers[name]</c>.</param>
    /// <param name="secret">The signing secret; defaults to <see cref="QasimhaClientOptions.WebhookSecret"/>.</param>
    /// <param name="options">Tolerance and clock.</param>
    /// <exception cref="WebhookSignatureException">The delivery failed verification.</exception>
    /// <exception cref="ConfigurationException">No secret is available.</exception>
    public WebhookEvent ConstructEvent(string rawBody, Func<string, string?> headers, string? secret = null, WebhookVerificationOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(rawBody);
        return ConstructEvent(Encoding.UTF8.GetBytes(rawBody), headers, secret, options);
    }

    /// <summary>Verifies a delivery and parses it, reading the headers through <paramref name="headers"/>.</summary>
    /// <param name="rawBody">The raw request body.</param>
    /// <param name="headers">Returns a request header's value by name, or <see langword="null"/>.</param>
    /// <param name="secret">The signing secret; defaults to <see cref="QasimhaClientOptions.WebhookSecret"/>.</param>
    /// <param name="options">Tolerance and clock.</param>
    /// <exception cref="WebhookSignatureException">The delivery failed verification.</exception>
    /// <exception cref="ConfigurationException">No secret is available.</exception>
    public WebhookEvent ConstructEvent(byte[] rawBody, Func<string, string?> headers, string? secret = null, WebhookVerificationOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(rawBody);
        ArgumentNullException.ThrowIfNull(headers);
        VerifySignature(rawBody, headers(WebhookVerifier.SignatureHeader), secret, headers(WebhookVerifier.TimestampHeader), options);
        return WebhookEvent.Parse(rawBody, headers);
    }

    /// <summary>Verifies a delivery and parses it from the two signature headers.</summary>
    /// <param name="rawBody">The raw request body.</param>
    /// <param name="signatureHeader">The <c>X-Qasimha-Signature</c> value.</param>
    /// <param name="timestampHeader">The <c>X-Qasimha-Timestamp</c> value, when present.</param>
    /// <param name="secret">The signing secret; defaults to <see cref="QasimhaClientOptions.WebhookSecret"/>.</param>
    /// <param name="options">Tolerance and clock.</param>
    /// <exception cref="WebhookSignatureException">The delivery failed verification.</exception>
    /// <exception cref="ConfigurationException">No secret is available.</exception>
    public WebhookEvent ConstructEvent(string rawBody, string? signatureHeader, string? timestampHeader, string? secret = null, WebhookVerificationOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(rawBody);
        return ConstructEvent(Encoding.UTF8.GetBytes(rawBody), signatureHeader, timestampHeader, secret, options);
    }

    /// <summary>Verifies a delivery and parses it from the two signature headers.</summary>
    /// <param name="rawBody">The raw request body.</param>
    /// <param name="signatureHeader">The <c>X-Qasimha-Signature</c> value.</param>
    /// <param name="timestampHeader">The <c>X-Qasimha-Timestamp</c> value, when present.</param>
    /// <param name="secret">The signing secret; defaults to <see cref="QasimhaClientOptions.WebhookSecret"/>.</param>
    /// <param name="options">Tolerance and clock.</param>
    /// <exception cref="WebhookSignatureException">The delivery failed verification.</exception>
    /// <exception cref="ConfigurationException">No secret is available.</exception>
    public WebhookEvent ConstructEvent(byte[] rawBody, string? signatureHeader, string? timestampHeader, string? secret = null, WebhookVerificationOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(rawBody);
        VerifySignature(rawBody, signatureHeader, secret, timestampHeader, options);
        return WebhookEvent.Parse(rawBody, null);
    }

    /// <summary>Verifies a signature without parsing the body. Throws on any failure; never returns a boolean.</summary>
    /// <param name="rawBody">The raw request body.</param>
    /// <param name="signatureHeader">The <c>X-Qasimha-Signature</c> value.</param>
    /// <param name="secret">The signing secret; defaults to <see cref="QasimhaClientOptions.WebhookSecret"/>.</param>
    /// <param name="timestampHeader">The <c>X-Qasimha-Timestamp</c> value, when present.</param>
    /// <param name="options">Tolerance and clock.</param>
    /// <exception cref="WebhookSignatureException">The delivery failed verification.</exception>
    /// <exception cref="ConfigurationException">No secret is available.</exception>
    public void VerifySignature(string rawBody, string? signatureHeader, string? secret = null, string? timestampHeader = null, WebhookVerificationOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(rawBody);
        VerifySignature(Encoding.UTF8.GetBytes(rawBody), signatureHeader, secret, timestampHeader, options);
    }

    /// <summary>Verifies a signature without parsing the body. Throws on any failure; never returns a boolean.</summary>
    /// <param name="rawBody">The raw request body.</param>
    /// <param name="signatureHeader">The <c>X-Qasimha-Signature</c> value.</param>
    /// <param name="secret">The signing secret; defaults to <see cref="QasimhaClientOptions.WebhookSecret"/>.</param>
    /// <param name="timestampHeader">The <c>X-Qasimha-Timestamp</c> value, when present.</param>
    /// <param name="options">Tolerance and clock.</param>
    /// <exception cref="WebhookSignatureException">The delivery failed verification.</exception>
    /// <exception cref="ConfigurationException">No secret is available.</exception>
    public void VerifySignature(byte[] rawBody, string? signatureHeader, string? secret = null, string? timestampHeader = null, WebhookVerificationOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(rawBody);
        var resolved = string.IsNullOrWhiteSpace(secret) ? _config.WebhookSecret : secret;
        if (string.IsNullOrWhiteSpace(resolved))
        {
            throw new ConfigurationException("No webhook secret: set QasimhaClientOptions.WebhookSecret or pass one to the call.");
        }

        WebhookVerifier.Verify(rawBody, signatureHeader, resolved, timestampHeader, options?.Tolerance, options?.Now);
    }
}
