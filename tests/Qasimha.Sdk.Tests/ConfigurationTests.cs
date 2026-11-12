using System.Reflection;
using Qasimha.Sdk.Tests.Support;
using Xunit;

namespace Qasimha.Sdk.Tests;

public class ConfigurationTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("pk_test_abc")]
    [InlineData("sk_test_")]
    [InlineData("sk_test_abc-def")]
    [InlineData("sk_prod_abc")]
    [InlineData("SK_TEST_abc")]
    public void Rejects_a_missing_or_malformed_key(string? key)
    {
        var ex = Assert.Throws<ConfigurationException>(() => new QasimhaClient(new QasimhaClientOptions { ApiKey = key }));
        Assert.DoesNotContain("abc", ex.Message);
    }

    [Fact]
    public void Infers_sandbox_from_a_test_key()
    {
        using var client = new QasimhaClient(new QasimhaClientOptions { ApiKey = "sk_test_abc123" });
        Assert.Equal(QasimhaEnvironment.Sandbox, client.Environment);
        Assert.Equal(new Uri("https://api-uat.qasimha.com"), client.BaseUrl);
    }

    [Fact]
    public void Infers_production_from_a_live_key()
    {
        using var client = new QasimhaClient(new QasimhaClientOptions { ApiKey = "sk_live_abc123" });
        Assert.Equal(QasimhaEnvironment.Production, client.Environment);
        Assert.Equal(new Uri("https://api.qasimha.com"), client.BaseUrl);
    }

    [Theory]
    [InlineData("sk_live_abc123", QasimhaEnvironment.Sandbox)]
    [InlineData("sk_test_abc123", QasimhaEnvironment.Production)]
    public void Rejects_an_environment_that_contradicts_the_key(string key, QasimhaEnvironment environment)
    {
        Assert.Throws<ConfigurationException>(() => new QasimhaClient(new QasimhaClientOptions { ApiKey = key, Environment = environment }));
    }

    [Theory]
    [InlineData("sk_live_abc123", QasimhaEnvironment.Production)]
    [InlineData("sk_test_abc123", QasimhaEnvironment.Sandbox)]
    public void Accepts_an_environment_that_agrees_with_the_key(string key, QasimhaEnvironment environment)
    {
        using var client = new QasimhaClient(new QasimhaClientOptions { ApiKey = key, Environment = environment });
        Assert.Equal(environment, client.Environment);
    }

    [Theory]
    [InlineData("http://api.example")]
    [InlineData("ftp://localhost")]
    [InlineData("not a url")]
    [InlineData("localhost:8080")]
    public void Rejects_a_base_url_that_is_not_https(string baseUrl)
    {
        Assert.Throws<ConfigurationException>(() => new QasimhaClient(new QasimhaClientOptions { ApiKey = "sk_test_abc", BaseUrl = baseUrl }));
    }

    [Theory]
    [InlineData("http://localhost:8080", "http://localhost:8080")]
    [InlineData("http://127.0.0.1:5000/", "http://127.0.0.1:5000")]
    [InlineData("https://localhost/prefix/", "https://localhost/prefix")]
    [InlineData("https://api.example/", "https://api.example")]
    public void Accepts_https_anywhere_and_http_on_loopback(string baseUrl, string expected)
    {
        using var client = new QasimhaClient(new QasimhaClientOptions { ApiKey = "sk_test_abc", BaseUrl = baseUrl });
        Assert.Equal(new Uri(expected), client.BaseUrl);
    }

    [Fact]
    public void A_base_url_override_skips_the_environment_check()
    {
        using var client = new QasimhaClient(new QasimhaClientOptions
        {
            ApiKey = "sk_live_abc123",
            Environment = QasimhaEnvironment.Sandbox,
            BaseUrl = "https://localhost",
        });
        Assert.Equal(QasimhaEnvironment.Sandbox, client.Environment);
        Assert.Equal(new Uri("https://localhost"), client.BaseUrl);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Rejects_a_non_positive_timeout(int seconds)
    {
        Assert.Throws<ConfigurationException>(() => new QasimhaClient(new QasimhaClientOptions { ApiKey = "sk_test_abc", Timeout = TimeSpan.FromSeconds(seconds) }));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(6)]
    public void Rejects_retries_outside_zero_to_five(int retries)
    {
        Assert.Throws<ConfigurationException>(() => new QasimhaClient(new QasimhaClientOptions { ApiKey = "sk_test_abc", MaxRetries = retries }));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(5)]
    public void Accepts_retries_at_the_bounds(int retries)
    {
        using var client = new QasimhaClient(new QasimhaClientOptions { ApiKey = "sk_test_abc", MaxRetries = retries });
        Assert.NotNull(client);
    }

    [Theory]
    [InlineData("fr")]
    [InlineData("ar-SA")]
    public void Rejects_an_unsupported_language(string language)
    {
        Assert.Throws<ConfigurationException>(() => new QasimhaClient(new QasimhaClientOptions { ApiKey = "sk_test_abc", Language = language }));
    }

    [Theory]
    [InlineData("sk_test_abc")]
    [InlineData("pk_test_")]
    [InlineData("pk_test_abc-def")]
    [InlineData("pk_prod_abc")]
    [InlineData("PK_TEST_abc")]
    public void Rejects_a_malformed_publishable_key(string publishableKey)
    {
        var ex = Assert.Throws<ConfigurationException>(() => new QasimhaClient(new QasimhaClientOptions { ApiKey = "sk_test_secret", PublishableKey = publishableKey }));
        Assert.DoesNotContain("abc", ex.Message);
    }

    [Theory]
    [InlineData("sk_test_abc123", "pk_live_abc123")]
    [InlineData("sk_live_abc123", "pk_test_abc123")]
    public void Rejects_a_publishable_key_from_the_other_environment(string apiKey, string publishableKey)
    {
        var ex = Assert.Throws<ConfigurationException>(() => new QasimhaClient(new QasimhaClientOptions { ApiKey = apiKey, PublishableKey = publishableKey }));
        Assert.DoesNotContain("abc123", ex.Message);
    }

    [Theory]
    [InlineData("sk_test_abc123", "pk_test_xyz789", "pk_test_…z789")]
    [InlineData("sk_live_abc123", " pk_live_xyz789 ", "pk_live_…z789")]
    public void Accepts_a_publishable_key_from_the_same_environment(string apiKey, string publishableKey, string masked)
    {
        using var client = new QasimhaClient(new QasimhaClientOptions { ApiKey = apiKey, PublishableKey = publishableKey });
        Assert.Equal(masked, client.MaskedPublishableKey);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void A_publishable_key_is_optional(string? publishableKey)
    {
        using var client = new QasimhaClient(new QasimhaClientOptions { ApiKey = "sk_test_abc", PublishableKey = publishableKey });
        Assert.Null(client.MaskedPublishableKey);
    }

    [Fact]
    public void Masks_the_key_and_never_prints_it()
    {
        using var harness = new Harness();
        Assert.Equal("sk_test_…3XYZ", harness.Client.MaskedKey);
        var text = harness.Client.ToString();
        Assert.Contains("sk_test_…3XYZ", text);
        Assert.Contains("sandbox", text);
        Assert.DoesNotContain(Harness.ApiKey, text);
        Assert.DoesNotContain(Harness.PublishableKey, text);
        Assert.Equal("pk_test_…6DEF", harness.Client.MaskedPublishableKey);
    }

    [Fact]
    public void A_short_key_masks_to_the_prefix_only()
    {
        using var client = new QasimhaClient(new QasimhaClientOptions { ApiKey = "sk_live_ab" });
        Assert.Equal("sk_live_…", client.MaskedKey);
    }

    [Fact]
    public void Version_matches_the_assembly()
    {
        var informational = typeof(QasimhaClient).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()!.InformationalVersion;
        var plus = informational.IndexOf('+', StringComparison.Ordinal);
        Assert.Equal(plus < 0 ? informational : informational[..plus], QasimhaClient.Version);
        Assert.Equal("0.1.0", QasimhaClient.Version);
    }

    [Fact]
    public void Rejects_null_arguments()
    {
        Assert.Throws<ArgumentNullException>(() => new QasimhaClient(null!));
        Assert.Throws<ArgumentNullException>(() => new QasimhaClient(new QasimhaClientOptions { ApiKey = "sk_test_abc" }, (HttpClient)null!));
        Assert.Throws<ArgumentNullException>(() => new QasimhaClient(new QasimhaClientOptions { ApiKey = "sk_test_abc" }, (HttpMessageHandler)null!));
    }

    [Fact]
    public async Task Does_not_dispose_a_caller_supplied_http_client()
    {
        using var http = new HttpClient(new FakeHandler().Enqueue(200, Fixtures.Envelope("SUCCESS", new { all = 1 })));
        var client = new QasimhaClient(new QasimhaClientOptions { ApiKey = "sk_test_abc" }, http);
        client.Dispose();
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://localhost/api/v1/merchant/orders/counts");
        using var response = await http.SendAsync(request);
        Assert.Equal(200, (int)response.StatusCode);
    }
}
