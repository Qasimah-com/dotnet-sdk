using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Qasimha.Sdk;

/// <summary>
/// Verifies webhook signatures in both formats the platform uses: <c>sha256=&lt;hex&gt;</c> over the raw
/// body, and <c>t=&lt;unix&gt;,v1=&lt;hex&gt;[,v1=…]</c> over <c>"&lt;t&gt;." + body</c>. Every comparison is
/// constant-time. Usable without a client; <see cref="WebhooksResource"/> wraps it with the client's secret.
/// </summary>
public static class WebhookVerifier
{
    /// <summary>The signature header.</summary>
    public const string SignatureHeader = "X-Qasimha-Signature";

    /// <summary>The timestamp header (unix seconds).</summary>
    public const string TimestampHeader = "X-Qasimha-Timestamp";

    /// <summary>The delivery id header.</summary>
    public const string EventIdHeader = "X-Qasimha-Event-Id";

    /// <summary>The event type header.</summary>
    public const string EventTypeHeader = "X-Qasimha-Event";

    /// <summary>The 1-based delivery attempt header, sent to registered endpoints.</summary>
    public const string DeliveryAttemptHeader = "X-Qasimha-Delivery-Attempt";

    private const string BodyOnlyPrefix = "sha256=";

    /// <summary>How far a delivery's timestamp may be from now before it is rejected: 300 seconds.</summary>
    public static TimeSpan DefaultTolerance { get; } = TimeSpan.FromSeconds(300);

    /// <summary>Verifies a signature. Throws on any failure; never returns a boolean.</summary>
    /// <param name="rawBody">The raw request body, exactly as received.</param>
    /// <param name="signatureHeader">The <c>X-Qasimha-Signature</c> value.</param>
    /// <param name="secret">The signing secret.</param>
    /// <param name="timestampHeader">The <c>X-Qasimha-Timestamp</c> value; when present with the <c>sha256=</c> format it is checked against the tolerance.</param>
    /// <param name="tolerance">Replay window; defaults to <see cref="DefaultTolerance"/>.</param>
    /// <param name="now">The current time; defaults to <see cref="DateTimeOffset.UtcNow"/>.</param>
    /// <exception cref="WebhookSignatureException">The header is missing or malformed, the timestamp is outside the tolerance, or no signature matches.</exception>
    /// <exception cref="ConfigurationException"><paramref name="secret"/> is empty.</exception>
    public static void Verify(
        ReadOnlySpan<byte> rawBody,
        string? signatureHeader,
        string secret,
        string? timestampHeader = null,
        TimeSpan? tolerance = null,
        DateTimeOffset? now = null)
    {
        if (string.IsNullOrWhiteSpace(secret))
        {
            throw new ConfigurationException("A webhook secret is required to verify a signature.");
        }

        if (string.IsNullOrWhiteSpace(signatureHeader))
        {
            throw new WebhookSignatureException(WebhookSignatureFailure.MissingHeader, "The " + SignatureHeader + " header is missing.");
        }

        var window = tolerance ?? DefaultTolerance;
        var clock = now ?? DateTimeOffset.UtcNow;
        var header = signatureHeader.Trim();
        var key = Encoding.UTF8.GetBytes(secret);

        if (header.StartsWith(BodyOnlyPrefix, StringComparison.OrdinalIgnoreCase))
        {
            VerifyBodyOnly(rawBody, header.Substring(BodyOnlyPrefix.Length), key, timestampHeader, window, clock);
            return;
        }

        if (header.Contains('=', StringComparison.Ordinal))
        {
            VerifyTimestamped(rawBody, header, key, window, clock);
            return;
        }

        throw new WebhookSignatureException(
            WebhookSignatureFailure.MalformedHeader,
            "The " + SignatureHeader + " header is neither sha256=<hex> nor t=<unix>,v1=<hex>.");
    }

    /// <summary>Verifies a signature over a body read as text. Throws on any failure; never returns a boolean.</summary>
    /// <param name="rawBody">The raw request body as read from the request stream.</param>
    /// <param name="signatureHeader">The <c>X-Qasimha-Signature</c> value.</param>
    /// <param name="secret">The signing secret.</param>
    /// <param name="timestampHeader">The <c>X-Qasimha-Timestamp</c> value, when present.</param>
    /// <param name="tolerance">Replay window; defaults to <see cref="DefaultTolerance"/>.</param>
    /// <param name="now">The current time; defaults to <see cref="DateTimeOffset.UtcNow"/>.</param>
    /// <exception cref="WebhookSignatureException">The header is missing or malformed, the timestamp is outside the tolerance, or no signature matches.</exception>
    /// <exception cref="ConfigurationException"><paramref name="secret"/> is empty.</exception>
    public static void Verify(
        string rawBody,
        string? signatureHeader,
        string secret,
        string? timestampHeader = null,
        TimeSpan? tolerance = null,
        DateTimeOffset? now = null)
    {
        ArgumentNullException.ThrowIfNull(rawBody);
        Verify(Encoding.UTF8.GetBytes(rawBody), signatureHeader, secret, timestampHeader, tolerance, now);
    }

    private static void VerifyBodyOnly(
        ReadOnlySpan<byte> body,
        string providedHex,
        byte[] key,
        string? timestampHeader,
        TimeSpan tolerance,
        DateTimeOffset now)
    {
        if (!string.IsNullOrWhiteSpace(timestampHeader))
        {
            if (!long.TryParse(timestampHeader.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var timestamp))
            {
                throw new WebhookSignatureException(
                    WebhookSignatureFailure.MalformedHeader,
                    "The " + TimestampHeader + " header is not a unix timestamp.");
            }

            EnsureWithinTolerance(timestamp, tolerance, now);
        }

        var expected = HMACSHA256.HashData(key, body);
        if (!Matches(expected, providedHex))
        {
            throw Mismatch();
        }
    }

    private static void VerifyTimestamped(ReadOnlySpan<byte> body, string header, byte[] key, TimeSpan tolerance, DateTimeOffset now)
    {
        long? timestamp = null;
        var signatures = new List<string>();

        foreach (var part in header.Split(','))
        {
            var pair = part.Trim();
            var separator = pair.IndexOf('=', StringComparison.Ordinal);
            if (separator <= 0)
            {
                throw new WebhookSignatureException(WebhookSignatureFailure.MalformedHeader, "The " + SignatureHeader + " header has a malformed element.");
            }

            var name = pair.Substring(0, separator);
            var value = pair.Substring(separator + 1);
            if (string.Equals(name, "t", StringComparison.Ordinal))
            {
                if (!long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
                {
                    throw new WebhookSignatureException(WebhookSignatureFailure.MalformedHeader, "The " + SignatureHeader + " header's t is not a unix timestamp.");
                }

                timestamp = parsed;
            }
            else if (string.Equals(name, "v1", StringComparison.Ordinal))
            {
                signatures.Add(value);
            }
        }

        if (timestamp is null || signatures.Count == 0)
        {
            throw new WebhookSignatureException(WebhookSignatureFailure.MalformedHeader, "The " + SignatureHeader + " header needs t and at least one v1.");
        }

        EnsureWithinTolerance(timestamp.Value, tolerance, now);

        var prefix = Encoding.UTF8.GetBytes(timestamp.Value.ToString(CultureInfo.InvariantCulture) + ".");
        var material = new byte[prefix.Length + body.Length];
        prefix.CopyTo(material, 0);
        body.CopyTo(material.AsSpan(prefix.Length));
        var expected = HMACSHA256.HashData(key, material);

        var matched = false;
        foreach (var signature in signatures)
        {
            matched |= Matches(expected, signature);
        }

        if (!matched)
        {
            throw Mismatch();
        }
    }

    private static void EnsureWithinTolerance(long timestamp, TimeSpan tolerance, DateTimeOffset now)
    {
        if (Math.Abs(now.ToUnixTimeSeconds() - timestamp) > tolerance.TotalSeconds)
        {
            throw new WebhookSignatureException(
                WebhookSignatureFailure.TimestampOutsideTolerance,
                "The delivery timestamp is outside the accepted tolerance.");
        }
    }

    private static bool Matches(byte[] expected, string providedHex) =>
        TryDecodeHex(providedHex.Trim(), out var provided)
        && provided.Length == expected.Length
        && CryptographicOperations.FixedTimeEquals(expected, provided);

    private static bool TryDecodeHex(string text, out byte[] bytes)
    {
        bytes = Array.Empty<byte>();
        if (text.Length == 0 || text.Length % 2 != 0)
        {
            return false;
        }

        var result = new byte[text.Length / 2];
        for (var i = 0; i < result.Length; i++)
        {
            var high = HexValue(text[2 * i]);
            var low = HexValue(text[(2 * i) + 1]);
            if (high < 0 || low < 0)
            {
                return false;
            }

            result[i] = (byte)((high << 4) | low);
        }

        bytes = result;
        return true;
    }

    private static int HexValue(char c) => c switch
    {
        >= '0' and <= '9' => c - '0',
        >= 'a' and <= 'f' => c - 'a' + 10,
        >= 'A' and <= 'F' => c - 'A' + 10,
        _ => -1,
    };

    private static WebhookSignatureException Mismatch() =>
        new(WebhookSignatureFailure.SignatureMismatch, "The webhook signature does not match.");
}
