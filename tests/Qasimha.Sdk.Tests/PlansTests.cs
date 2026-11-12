using Qasimha.Sdk.Tests.Support;
using Xunit;

namespace Qasimha.Sdk.Tests;

public class PlansTests
{
    [Fact]
    public async Task Quotes_plans_from_the_recorded_fixture()
    {
        using var harness = new Harness();
        harness.Handler.Enqueue(200, Fixtures.Read("widget-plans.json"));

        var quote = await harness.Client.Checkout.GetPlansAsync(1200.00m);

        Assert.Equal(HttpMethod.Get, harness.LastRequest.Method);
        Assert.Equal("/api/v1/widget/plans", harness.LastRequest.Uri.AbsolutePath);
        Assert.Equal("?amount=1200.00&currency=SAR&lang=en", harness.LastRequest.Uri.Query);

        Assert.Equal("Cart API Test 260924075803", quote.MerchantName);
        Assert.Equal("SAR", quote.Currency);
        Assert.Equal(100m, quote.MinAmount);
        Assert.Equal(4000m, quote.MaxAmount);
        Assert.Equal(3, quote.Items.Count);
        Assert.Equal(new[] { 2, 3, 4 }, quote.Items.Select(p => p.Instalments));

        var three = quote.Items[1];
        Assert.Equal("monthly", three.Frequency);
        Assert.Equal(409.58m, three.InstalmentAmount);
        Assert.Equal(409.58m, three.FirstInstalmentAmount);
        Assert.Equal(409.59m, three.LastInstalmentAmount);
        Assert.Equal(28.75m, three.FeesIncludingVat);
        Assert.Equal(1228.75m, three.TotalPayable);
        Assert.True(three.ShariaCompliant);

        Assert.Equal("76eeb42b-b7b1-4bc7-9395-dae97215584e", quote.RequestId);
        Assert.Equal(200, quote.Response!.HttpStatus);
        Assert.Equal(3, quote.Raw.GetProperty("plans").GetArrayLength());
    }

    [Fact]
    public async Task Sends_the_publishable_key_and_not_the_api_key()
    {
        using var harness = new Harness(o => o.UserAgentSuffix = "shop/2.0");
        harness.Handler.Enqueue(200, Fixtures.Read("widget-plans.json"));

        await harness.Client.Checkout.GetPlansAsync(1200.00m);

        var request = harness.LastRequest;
        Assert.Equal(Harness.PublishableKey, request.Header("X-Public-Key"));
        Assert.Null(request.Header("X-Api-Key"));
        Assert.Equal("en", request.Header("Accept-Language"));
        Assert.Equal("application/json", request.Header("Accept"));
        Assert.Null(request.Header("Idempotency-Key"));
        Assert.Null(request.Body);
        Assert.True(Guid.TryParse(request.Header("X-Correlation-Id"), out _));
        Assert.StartsWith("qasimha-sdk-dotnet/", request.Header("User-Agent"), StringComparison.Ordinal);
        Assert.EndsWith(" shop/2.0", request.Header("User-Agent"), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Other_calls_keep_the_api_key()
    {
        using var harness = new Harness();
        harness.Handler
            .Enqueue(200, Fixtures.Read("widget-plans.json"))
            .Enqueue(200, Fixtures.Envelope("SUCCESS", new { all = 1 }));

        await harness.Client.Checkout.GetPlansAsync(1200.00m);
        await harness.Client.Orders.CountsAsync();

        Assert.Equal(Harness.ApiKey, harness.LastRequest.Header("X-Api-Key"));
        Assert.Null(harness.LastRequest.Header("X-Public-Key"));
    }

    [Fact]
    public async Task Per_call_options_override_the_language_and_the_key()
    {
        using var harness = new Harness();
        harness.Handler.Enqueue(200, Fixtures.Read("widget-plans.json"));

        await harness.Client.Checkout.GetPlansAsync(250.50m, new GetPlansOptions { Lang = "AR", PublishableKey = " pk_test_other789 " });

        var request = harness.LastRequest;
        Assert.Equal("?amount=250.50&currency=SAR&lang=ar", request.Uri.Query);
        Assert.Equal("ar", request.Header("Accept-Language"));
        Assert.Equal("pk_test_other789", request.Header("X-Public-Key"));
        Assert.Null(request.Header("X-Api-Key"));
    }

    [Fact]
    public async Task A_per_call_key_serves_a_client_without_one()
    {
        using var harness = new Harness(o => o.PublishableKey = null);
        harness.Handler.Enqueue(200, Fixtures.Read("widget-plans.json"));

        var quote = await harness.Client.Checkout.GetPlansAsync(1200m, new GetPlansOptions { PublishableKey = "pk_test_percall1" });

        Assert.Equal(3, quote.Items.Count);
        Assert.Equal("pk_test_percall1", harness.LastRequest.Header("X-Public-Key"));
    }

    [Fact]
    public async Task An_empty_plan_list_is_returned_not_thrown()
    {
        using var harness = new Harness();
        harness.Handler.Enqueue(200, Fixtures.Envelope("OK", new
        {
            merchantName = "Shop",
            currency = "SAR",
            minAmount = 100,
            maxAmount = 4000,
            plans = Array.Empty<object>(),
        }, message: "Plans quoted"));

        var quote = await harness.Client.Checkout.GetPlansAsync(50m);

        Assert.Empty(quote.Items);
        Assert.Equal(100m, quote.MinAmount);
        Assert.Equal(4000m, quote.MaxAmount);
        Assert.Equal("?amount=50.00&currency=SAR&lang=en", harness.LastRequest.Uri.Query);
    }

    [Fact]
    public async Task A_null_plan_list_is_empty()
    {
        using var harness = new Harness();
        harness.Handler.Enqueue(200, Fixtures.Envelope("OK", new { merchantName = "Shop", currency = "SAR", minAmount = 100, maxAmount = 4000, plans = (object?)null }));

        var quote = await harness.Client.Checkout.GetPlansAsync(50m);

        Assert.Empty(quote.Items);
    }

    [Fact]
    public async Task A_401_is_an_authentication_error()
    {
        using var harness = new Harness();
        harness.Handler.Enqueue(401, Fixtures.Envelope("INVALID_PUBLIC_KEY", null, success: false, message: "Unknown publishable key"));

        var ex = await Assert.ThrowsAsync<AuthenticationException>(() => harness.Client.Checkout.GetPlansAsync(1200m));

        Assert.Equal(401, ex.HttpStatus);
        Assert.Equal("INVALID_PUBLIC_KEY", ex.Code);
        Assert.Equal("Unknown publishable key", ex.Message);
        Assert.Single(harness.Handler.Requests);
        Assert.DoesNotContain(Harness.PublishableKey, ex.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_401_with_an_empty_body_is_still_an_authentication_error()
    {
        using var harness = new Harness();
        harness.Handler.Enqueue(401, null, r => r.Headers.Add("WWW-Authenticate", "ApiKey realm=\"merchant-api\""));

        var ex = await Assert.ThrowsAsync<AuthenticationException>(() => harness.Client.Checkout.GetPlansAsync(1200m));

        Assert.Null(ex.Code);
        Assert.Contains("401", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_400_is_a_validation_error()
    {
        using var harness = new Harness();
        harness.Handler.Enqueue(400, Fixtures.Envelope("INVALID_AMOUNT", null, success: false, message: "Amount must be positive"));

        var ex = await Assert.ThrowsAsync<ValidationException>(() => harness.Client.Checkout.GetPlansAsync(0m));

        Assert.Equal(400, ex.HttpStatus);
        Assert.Equal("INVALID_AMOUNT", ex.Code);
        Assert.Equal("Amount must be positive", ex.Message);
        Assert.Single(harness.Handler.Requests);
    }

    [Fact]
    public async Task A_missing_publishable_key_is_a_configuration_error_before_any_request()
    {
        using var harness = new Harness(o => o.PublishableKey = null);

        var ex = await Assert.ThrowsAsync<ConfigurationException>(() => harness.Client.Checkout.GetPlansAsync(1200m));

        Assert.Contains("PublishableKey", ex.Message, StringComparison.Ordinal);
        Assert.Empty(harness.Handler.Requests);
        Assert.Empty(harness.Logs);
    }

    [Theory]
    [InlineData("pk_live_other789")]
    [InlineData("sk_test_other789")]
    [InlineData("pk_test_")]
    public async Task A_bad_per_call_key_is_a_configuration_error_before_any_request(string publishableKey)
    {
        using var harness = new Harness();

        var ex = await Assert.ThrowsAsync<ConfigurationException>(() =>
            harness.Client.Checkout.GetPlansAsync(1200m, new GetPlansOptions { PublishableKey = publishableKey }));

        Assert.DoesNotContain("other789", ex.Message, StringComparison.Ordinal);
        Assert.Empty(harness.Handler.Requests);
    }

    [Fact]
    public async Task An_unsupported_language_is_a_configuration_error_before_any_request()
    {
        using var harness = new Harness();

        await Assert.ThrowsAsync<ConfigurationException>(() =>
            harness.Client.Checkout.GetPlansAsync(1200m, new GetPlansOptions { Lang = "fr" }));

        Assert.Empty(harness.Handler.Requests);
    }

    [Theory]
    [InlineData("1200.005")]
    [InlineData("0.001")]
    [InlineData("-1.00")]
    public async Task An_amount_outside_the_money_rules_is_refused_locally(string amount)
    {
        using var harness = new Harness();

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            harness.Client.Checkout.GetPlansAsync(decimal.Parse(amount, System.Globalization.CultureInfo.InvariantCulture)));

        var error = Assert.Single(ex.Errors);
        Assert.Equal("Amount", error.Field);
        Assert.Null(ex.HttpStatus);
        Assert.Empty(harness.Handler.Requests);
    }

    [Fact]
    public async Task Trailing_zeros_are_not_extra_decimals()
    {
        using var harness = new Harness();
        harness.Handler.Enqueue(200, Fixtures.Read("widget-plans.json"));

        await harness.Client.Checkout.GetPlansAsync(1200.000m);

        Assert.Equal("?amount=1200.00&currency=SAR&lang=en", harness.LastRequest.Uri.Query);
    }

    [Fact]
    public async Task A_429_is_retried_like_every_get()
    {
        using var harness = new Harness();
        harness.Handler
            .Enqueue(429, null, r => r.Headers.Add("Retry-After", "3"))
            .Enqueue(200, Fixtures.Read("widget-plans.json"));

        var quote = await harness.Client.Checkout.GetPlansAsync(1200m);

        Assert.Equal(3, quote.Items.Count);
        Assert.Equal(2, quote.Response!.Attempt);
        Assert.Equal(2, harness.Handler.Requests.Count);
        Assert.Equal(new[] { TimeSpan.FromSeconds(3) }, harness.Delay.Waits);
        Assert.All(harness.Handler.Requests, r =>
        {
            Assert.Equal(Harness.PublishableKey, r.Header("X-Public-Key"));
            Assert.Null(r.Header("X-Api-Key"));
        });
        Assert.Single(harness.Handler.Requests.Select(r => r.Header("X-Correlation-Id")).Distinct());
    }

    [Fact]
    public async Task A_503_then_a_429_after_the_retries_is_a_rate_limit_error()
    {
        using var harness = new Harness(o => o.MaxRetries = 1);
        harness.Handler.Enqueue(503).Enqueue(429, Fixtures.Envelope("RATE_LIMITED", null, success: false));

        var ex = await Assert.ThrowsAsync<RateLimitException>(() => harness.Client.Checkout.GetPlansAsync(1200m));

        Assert.Equal(429, ex.HttpStatus);
        Assert.Equal(2, harness.Handler.Requests.Count);
    }

    [Fact]
    public async Task The_publishable_key_never_reaches_the_log()
    {
        using var harness = new Harness();
        harness.Handler.Enqueue(503).EnqueueThrow(new HttpRequestException("reset")).Enqueue(401);

        await Assert.ThrowsAsync<AuthenticationException>(() =>
            harness.Client.Checkout.GetPlansAsync(1200m, new GetPlansOptions { PublishableKey = "pk_test_percall1" }));

        Assert.Equal(3, harness.Logs.Count);
        var text = string.Join("\n", harness.Logs.Select(e => e.Message + " " + string.Join(" ", e.Fields.Select(f => f.Key + "=" + f.Value))));
        Assert.DoesNotContain(Harness.PublishableKey, text, StringComparison.Ordinal);
        Assert.DoesNotContain("pk_test_percall1", text, StringComparison.Ordinal);
        Assert.DoesNotContain(Harness.ApiKey, text, StringComparison.Ordinal);
        Assert.DoesNotContain("1200", text, StringComparison.Ordinal);
        Assert.All(harness.Logs, e => Assert.Equal("/api/v1/widget/plans", e.Fields["path"]));
    }
}
