using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Qasimha.Sdk.Tests.Support;
using Xunit;

namespace Qasimha.Sdk.Tests;

public class WebhookTests
{
    private static readonly Vectors V = Vectors.Load();

    private static readonly JsonSerializerOptions Indented = new() { WriteIndented = true };

    [Fact]
    public void The_v0_vector_is_the_hmac_of_the_body()
    {
        var expected = "sha256=" + Convert.ToHexString(HMACSHA256.HashData(Encoding.UTF8.GetBytes(V.Secret), Encoding.UTF8.GetBytes(V.V0Body))).ToLowerInvariant();

        Assert.Equal(V.V0Signature, expected);
        Assert.Equal("sha256=30babb40a6df2b6d212dc38bbeb7af79a6517cd037368354dd9e44eda1b9a092", V.V0Signature);
    }

    [Fact]
    public void The_v1_vector_is_the_hmac_of_the_timestamp_and_body()
    {
        var material = Encoding.UTF8.GetBytes(V.Timestamp + "." + V.V1Body);
        var expected = "t=" + V.Timestamp + ",v1=" + Convert.ToHexString(HMACSHA256.HashData(Encoding.UTF8.GetBytes(V.Secret), material)).ToLowerInvariant();

        Assert.Equal(V.V1Signature, expected);
        Assert.Equal("t=1758790800,v1=1eb16bb179f2c348a6e3d889fc70f71e9656aa50636e8d19de4de28cd03f337b", V.V1Signature);
    }

    [Fact]
    public void Constructs_a_v0_event_from_headers()
    {
        using var harness = new Harness();

        var evt = harness.Client.Webhooks.ConstructEvent(V.V0Body, V.V0Header, options: new WebhookVerificationOptions { Now = V.NowWithin });

        Assert.Equal("evt_0123456789abcdef0123456789abcdef", evt.Id);
        Assert.Equal("order.completed", evt.Type);
        Assert.Null(evt.OrderId);
        Assert.Equal("ORDER-1", evt.OrderReferenceId);
        Assert.Equal("Confirmed", evt.Status);
        Assert.Equal(1200.00m, evt.Amount);
        Assert.Equal("SAR", evt.Currency);
        Assert.Equal(new DateTimeOffset(2026, 9, 25, 9, 0, 0, TimeSpan.Zero), evt.OccurredAt);
        Assert.Null(evt.DeliveryAttempt);
        Assert.Equal(JsonValueKind.Object, evt.Raw.ValueKind);
    }

    [Fact]
    public void Constructs_a_v1_event_from_headers()
    {
        using var harness = new Harness();
        var headers = new Dictionary<string, string>(V.V1Headers, StringComparer.OrdinalIgnoreCase) { ["X-Qasimha-Delivery-Attempt"] = "2" };

        var evt = harness.Client.Webhooks.ConstructEvent(V.V1Body, name => headers.TryGetValue(name, out var value) ? value : null, options: new WebhookVerificationOptions { Now = V.NowWithin });

        Assert.Equal("evt_1", evt.Id);
        Assert.Equal("order.confirmed", evt.Type);
        Assert.Equal("9a2e6c4b-1111-4c2d-8a1f-000000000001", evt.OrderId);
        Assert.Equal("ORDER-1", evt.OrderReferenceId);
        Assert.Equal("Confirmed", evt.Status);
        Assert.Equal(1200.00m, evt.Amount);
        Assert.Equal(new DateTimeOffset(2026, 9, 25, 9, 0, 0, TimeSpan.Zero), evt.OccurredAt);
        Assert.Equal(2, evt.DeliveryAttempt);
        Assert.Equal(1200.00m, evt.Raw.GetProperty("data").GetProperty("order").GetProperty("totalAmount").GetDecimal());
    }

    [Fact]
    public void Constructs_from_bytes_and_explicit_headers()
    {
        using var harness = new Harness();

        var evt = harness.Client.Webhooks.ConstructEvent(Encoding.UTF8.GetBytes(V.V1Body), V.V1Signature, V.TimestampHeader, options: new WebhookVerificationOptions { Now = V.NowWithin });

        Assert.Equal("evt_1", evt.Id);
    }

    [Fact]
    public void Accepts_a_rotated_header_with_two_signatures()
    {
        using var harness = new Harness();

        harness.Client.Webhooks.VerifySignature(V.V1Body, V.V1Rotated, options: new WebhookVerificationOptions { Now = V.NowWithin });
    }

    [Fact]
    public void Accepts_uppercase_hex()
    {
        using var harness = new Harness();

        harness.Client.Webhooks.VerifySignature(V.V0Body, V.V0Signature.ToUpperInvariant().Replace("SHA256=", "sha256=", StringComparison.Ordinal));
    }

    [Fact]
    public void Rejects_a_v1_timestamp_outside_the_tolerance()
    {
        using var harness = new Harness();

        var ex = Assert.Throws<WebhookSignatureException>(() =>
            harness.Client.Webhooks.VerifySignature(V.V1Body, V.V1Signature, options: new WebhookVerificationOptions { Now = V.NowOutside }));

        Assert.Equal(WebhookSignatureFailure.TimestampOutsideTolerance, ex.Reason);
        Assert.Equal("timestamp_outside_tolerance", ex.ReasonCode);
    }

    [Fact]
    public void Rejects_a_v0_timestamp_header_outside_the_tolerance()
    {
        using var harness = new Harness();

        var ex = Assert.Throws<WebhookSignatureException>(() =>
            harness.Client.Webhooks.VerifySignature(V.V0Body, V.V0Signature, timestampHeader: V.TimestampHeader, options: new WebhookVerificationOptions { Now = V.NowOutside }));

        Assert.Equal(WebhookSignatureFailure.TimestampOutsideTolerance, ex.Reason);
    }

    [Fact]
    public void A_v0_delivery_without_a_timestamp_is_not_replay_checked()
    {
        using var harness = new Harness();

        harness.Client.Webhooks.VerifySignature(V.V0Body, V.V0Signature, options: new WebhookVerificationOptions { Now = V.NowOutside });
    }

    [Fact]
    public void A_wider_tolerance_accepts_an_older_delivery()
    {
        using var harness = new Harness();

        harness.Client.Webhooks.VerifySignature(V.V1Body, V.V1Signature, options: new WebhookVerificationOptions { Now = V.NowOutside, Tolerance = TimeSpan.FromSeconds(1000) });
    }

    [Fact]
    public void The_tolerance_boundary_is_inclusive()
    {
        using var harness = new Harness();
        var edge = DateTimeOffset.FromUnixTimeSeconds(long.Parse(V.Timestamp, System.Globalization.CultureInfo.InvariantCulture) + 300);

        harness.Client.Webhooks.VerifySignature(V.V1Body, V.V1Signature, options: new WebhookVerificationOptions { Now = edge });
        Assert.Throws<WebhookSignatureException>(() =>
            harness.Client.Webhooks.VerifySignature(V.V1Body, V.V1Signature, options: new WebhookVerificationOptions { Now = edge.AddSeconds(1) }));
    }

    [Fact]
    public void Rejects_a_tampered_body()
    {
        using var harness = new Harness();

        var ex = Assert.Throws<WebhookSignatureException>(() =>
            harness.Client.Webhooks.VerifySignature(V.V0Body.Replace("1200.00", "1.00", StringComparison.Ordinal), V.V0Signature));

        Assert.Equal(WebhookSignatureFailure.SignatureMismatch, ex.Reason);
        Assert.Equal("signature_mismatch", ex.ReasonCode);
    }

    [Fact]
    public void Rejects_a_re_serialised_body()
    {
        using var harness = new Harness();
        using var document = JsonDocument.Parse(V.V0Body);
        var reserialised = JsonSerializer.Serialize(document.RootElement, Indented);

        Assert.NotEqual(V.V0Body, reserialised);
        Assert.Equal(document.RootElement.GetProperty("amount").GetDecimal(), JsonDocument.Parse(reserialised).RootElement.GetProperty("amount").GetDecimal());
        Assert.Throws<WebhookSignatureException>(() => harness.Client.Webhooks.VerifySignature(reserialised, V.V0Signature));
    }

    [Fact]
    public void Rejects_the_wrong_secret()
    {
        using var harness = new Harness();

        var ex = Assert.Throws<WebhookSignatureException>(() => harness.Client.Webhooks.VerifySignature(V.V0Body, V.V0Signature, secret: "whsec_other"));

        Assert.Equal(WebhookSignatureFailure.SignatureMismatch, ex.Reason);
    }

    [Theory]
    [InlineData("sha256=30babb40")]
    [InlineData("sha256=zz")]
    [InlineData("sha256=")]
    [InlineData("sha256=30babb40a6df2b6d212dc38bbeb7af79a6517cd037368354dd9e44eda1b9a09")]
    public void A_signature_of_the_wrong_length_or_alphabet_is_a_mismatch(string header)
    {
        using var harness = new Harness();

        var ex = Assert.Throws<WebhookSignatureException>(() => harness.Client.Webhooks.VerifySignature(V.V0Body, header));

        Assert.Equal(WebhookSignatureFailure.SignatureMismatch, ex.Reason);
    }

    [Fact]
    public void A_rotated_header_with_no_valid_signature_is_a_mismatch()
    {
        using var harness = new Harness();
        var header = "t=" + V.Timestamp + ",v1=" + new string('0', 64) + ",v1=" + new string('f', 64);

        var ex = Assert.Throws<WebhookSignatureException>(() =>
            harness.Client.Webhooks.VerifySignature(V.V1Body, header, options: new WebhookVerificationOptions { Now = V.NowWithin }));

        Assert.Equal(WebhookSignatureFailure.SignatureMismatch, ex.Reason);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void A_missing_header_is_missing_header(string? header)
    {
        using var harness = new Harness();

        var ex = Assert.Throws<WebhookSignatureException>(() => harness.Client.Webhooks.VerifySignature(V.V0Body, header));

        Assert.Equal(WebhookSignatureFailure.MissingHeader, ex.Reason);
        Assert.Equal("missing_header", ex.ReasonCode);
    }

    [Theory]
    [InlineData("garbage")]
    [InlineData("md5=abc")]
    [InlineData("t=abc,v1=1eb16bb179f2c348a6e3d889fc70f71e9656aa50636e8d19de4de28cd03f337b")]
    [InlineData("t=1758790800")]
    [InlineData("v1=1eb16bb179f2c348a6e3d889fc70f71e9656aa50636e8d19de4de28cd03f337b")]
    [InlineData("t=1758790800,=abc")]
    public void A_malformed_header_is_malformed_header(string header)
    {
        using var harness = new Harness();

        var ex = Assert.Throws<WebhookSignatureException>(() =>
            harness.Client.Webhooks.VerifySignature(V.V1Body, header, options: new WebhookVerificationOptions { Now = V.NowWithin }));

        Assert.Equal(WebhookSignatureFailure.MalformedHeader, ex.Reason);
        Assert.Equal("malformed_header", ex.ReasonCode);
    }

    [Fact]
    public void A_non_numeric_v0_timestamp_header_is_malformed()
    {
        using var harness = new Harness();

        var ex = Assert.Throws<WebhookSignatureException>(() => harness.Client.Webhooks.VerifySignature(V.V0Body, V.V0Signature, timestampHeader: "yesterday"));

        Assert.Equal(WebhookSignatureFailure.MalformedHeader, ex.Reason);
    }

    [Fact]
    public void Unknown_elements_in_a_v1_header_are_ignored()
    {
        using var harness = new Harness();

        harness.Client.Webhooks.VerifySignature(V.V1Body, V.V1Signature + ",v2=abc", options: new WebhookVerificationOptions { Now = V.NowWithin });
    }

    [Fact]
    public void Needs_a_secret_from_somewhere()
    {
        using var harness = new Harness(o => o.WebhookSecret = null);

        Assert.Throws<ConfigurationException>(() => harness.Client.Webhooks.VerifySignature(V.V0Body, V.V0Signature));
        harness.Client.Webhooks.VerifySignature(V.V0Body, V.V0Signature, secret: V.Secret);
    }

    [Fact]
    public void Verifies_without_a_client()
    {
        WebhookVerifier.Verify(V.V0Body, V.V0Signature, V.Secret);
        WebhookVerifier.Verify(Encoding.UTF8.GetBytes(V.V1Body), V.V1Signature, V.Secret, now: V.NowWithin);
        Assert.Throws<ConfigurationException>(() => WebhookVerifier.Verify(V.V0Body, V.V0Signature, ""));
    }

    [Fact]
    public void A_signed_body_that_is_not_json_is_a_validation_error()
    {
        using var harness = new Harness();
        var body = "not json";
        var header = "sha256=" + Convert.ToHexString(HMACSHA256.HashData(Encoding.UTF8.GetBytes(V.Secret), Encoding.UTF8.GetBytes(body))).ToLowerInvariant();

        var ex = Assert.Throws<ValidationException>(() => harness.Client.Webhooks.ConstructEvent(body, header, null));

        Assert.Equal("body", Assert.Single(ex.Errors).Field);
    }

    [Fact]
    public void A_signed_array_body_is_a_validation_error()
    {
        using var harness = new Harness();
        var body = "[1,2]";
        var header = "sha256=" + Convert.ToHexString(HMACSHA256.HashData(Encoding.UTF8.GetBytes(V.Secret), Encoding.UTF8.GetBytes(body))).ToLowerInvariant();

        Assert.Throws<ValidationException>(() => harness.Client.Webhooks.ConstructEvent(body, header, null));
    }

    [Fact]
    public void Falls_back_to_the_headers_for_id_and_type()
    {
        using var harness = new Harness();
        var body = """{"orderReferenceId":"ORDER-7","status":3}""";
        var header = "sha256=" + Convert.ToHexString(HMACSHA256.HashData(Encoding.UTF8.GetBytes(V.Secret), Encoding.UTF8.GetBytes(body))).ToLowerInvariant();
        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["X-Qasimha-Signature"] = header,
            ["X-Qasimha-Event-Id"] = "evt_h",
            ["X-Qasimha-Event"] = "order.declined",
        };

        var evt = harness.Client.Webhooks.ConstructEvent(body, name => headers.TryGetValue(name, out var v) ? v : null);

        Assert.Equal("evt_h", evt.Id);
        Assert.Equal("order.declined", evt.Type);
        Assert.Equal(OrderStatus.Declined, evt.Status);
        Assert.Null(evt.Amount);
    }

    [Fact]
    public void Rejects_null_arguments()
    {
        using var harness = new Harness();

        Assert.Throws<ArgumentNullException>(() => harness.Client.Webhooks.ConstructEvent((string)null!, V.V0Header));
        Assert.Throws<ArgumentNullException>(() => harness.Client.Webhooks.ConstructEvent(V.V0Body, (Func<string, string?>)null!));
        Assert.Throws<ArgumentNullException>(() => harness.Client.Webhooks.VerifySignature((byte[])null!, V.V0Signature));
    }

    private sealed class Vectors
    {
        public string Secret { get; private init; } = string.Empty;

        public string Timestamp { get; private init; } = string.Empty;

        public string TimestampHeader { get; private init; } = string.Empty;

        public string V0Body { get; private init; } = string.Empty;

        public string V0Signature { get; private init; } = string.Empty;

        public Dictionary<string, string> V0Headers { get; private init; } = new();

        public string V1Body { get; private init; } = string.Empty;

        public string V1Signature { get; private init; } = string.Empty;

        public string V1Rotated { get; private init; } = string.Empty;

        public Dictionary<string, string> V1Headers { get; private init; } = new();

        public DateTimeOffset NowWithin { get; private init; }

        public DateTimeOffset NowOutside { get; private init; }

        public Func<string, string?> V0Header => name => V0Headers.TryGetValue(name, out var value) ? value : null;

        public static Vectors Load()
        {
            using var document = Fixtures.Json("webhook-signature.json");
            var root = document.RootElement;
            var v0 = root.GetProperty("v0");
            var v1 = root.GetProperty("v1");
            return new Vectors
            {
                Secret = root.GetProperty("secret").GetString()!,
                Timestamp = root.GetProperty("timestamp").GetRawText(),
                TimestampHeader = v0.GetProperty("timestampHeader").GetString()!,
                V0Body = v0.GetProperty("body").GetString()!,
                V0Signature = v0.GetProperty("signatureHeader").GetString()!,
                V0Headers = Headers(v0.GetProperty("headers")),
                V1Body = v1.GetProperty("body").GetString()!,
                V1Signature = v1.GetProperty("signatureHeader").GetString()!,
                V1Rotated = v1.GetProperty("rotated").GetString()!,
                V1Headers = Headers(v1.GetProperty("headers")),
                NowWithin = DateTimeOffset.FromUnixTimeSeconds(root.GetProperty("nowWithinTolerance").GetInt64()),
                NowOutside = DateTimeOffset.FromUnixTimeSeconds(root.GetProperty("nowOutsideTolerance").GetInt64()),
            };
        }

        private static Dictionary<string, string> Headers(JsonElement element)
        {
            var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var property in element.EnumerateObject())
            {
                headers[property.Name] = property.Value.GetString()!;
            }

            return headers;
        }
    }
}
