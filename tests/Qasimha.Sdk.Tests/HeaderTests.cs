using Qasimha.Sdk.Tests.Support;
using Xunit;

namespace Qasimha.Sdk.Tests;

public class HeaderTests
{
    [Fact]
    public async Task A_post_carries_every_required_header()
    {
        using var harness = new Harness(o => o.UserAgentSuffix = "woocommerce/1.2.0");
        harness.Handler.Enqueue(201, Fixtures.SessionCreated);

        await harness.Client.Checkout.CreateSessionAsync(Baskets.ThreeLine());

        var request = harness.LastRequest;
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal("https://api-uat.qasimha.com/api/v1/pay/session/create", request.Uri.ToString());
        Assert.Equal(Harness.ApiKey, request.Header("X-Api-Key"));
        Assert.Equal("application/json", request.Header("Accept"));
        Assert.Equal("application/json; charset=utf-8", request.Header("Content-Type"));
        Assert.Equal("en", request.Header("Accept-Language"));
        Assert.True(Guid.TryParse(request.Header("Idempotency-Key"), out _));
        Assert.True(Guid.TryParse(request.Header("X-Correlation-Id"), out _));

        var userAgent = request.Header("User-Agent")!;
        Assert.StartsWith("qasimha-sdk-dotnet/0.1.0 (.NET/", userAgent, StringComparison.Ordinal);
        Assert.EndsWith(") woocommerce/1.2.0", userAgent, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_get_carries_no_body_and_no_idempotency_key()
    {
        using var harness = new Harness();
        harness.Handler.Enqueue(200, Fixtures.Read("orders-list.json"));

        await harness.Client.Orders.ListAsync();

        var request = harness.LastRequest;
        Assert.Equal(HttpMethod.Get, request.Method);
        Assert.Null(request.Body);
        Assert.Null(request.Header("Content-Type"));
        Assert.Null(request.Header("Idempotency-Key"));
        Assert.True(Guid.TryParse(request.Header("X-Correlation-Id"), out _));
        Assert.Equal(Harness.ApiKey, request.Header("X-Api-Key"));
    }

    [Fact]
    public async Task Sends_the_configured_language()
    {
        using var harness = new Harness(o => o.Language = "AR");
        harness.Handler.Enqueue(200, Fixtures.Read("orders-list.json"));

        await harness.Client.Orders.ListAsync();

        Assert.Equal("ar", harness.LastRequest.Header("Accept-Language"));
    }

    [Fact]
    public async Task Uses_a_caller_supplied_idempotency_key_verbatim()
    {
        using var harness = new Harness();
        harness.Handler.Enqueue(201, Fixtures.SessionCreated);

        await harness.Client.Checkout.CreateSessionAsync(Baskets.ThreeLine(), new RequestOptions { IdempotencyKey = "order-12345-attempt-1" });

        Assert.Equal("order-12345-attempt-1", harness.LastRequest.Header("Idempotency-Key"));
    }

    [Fact]
    public async Task Omits_the_idempotency_key_when_disabled()
    {
        using var harness = new Harness();
        harness.Handler.Enqueue(201, Fixtures.SessionCreated);

        await harness.Client.Checkout.CreateSessionAsync(Baskets.ThreeLine(), new RequestOptions { Idempotent = false });

        Assert.Null(harness.LastRequest.Header("Idempotency-Key"));
    }

    [Fact]
    public async Task Generates_a_fresh_key_per_logical_call()
    {
        using var harness = new Harness();
        harness.Handler.Enqueue(201, Fixtures.SessionCreated).Enqueue(201, Fixtures.SessionCreated);

        await harness.Client.Checkout.CreateSessionAsync(Baskets.ThreeLine("A"));
        await harness.Client.Checkout.CreateSessionAsync(Baskets.ThreeLine("B"));

        Assert.NotEqual(harness.Handler.Requests[0].Header("Idempotency-Key"), harness.Handler.Requests[1].Header("Idempotency-Key"));
        Assert.NotEqual(harness.Handler.Requests[0].Header("X-Correlation-Id"), harness.Handler.Requests[1].Header("X-Correlation-Id"));
    }

    [Fact]
    public async Task Keeps_a_base_url_path_prefix()
    {
        using var harness = new Harness(o => o.BaseUrl = "https://localhost/prefix/");
        harness.Handler.Enqueue(200, Fixtures.Envelope("SUCCESS", new { all = 1 }));

        await harness.Client.Orders.CountsAsync();

        Assert.Equal("https://localhost/prefix/api/v1/merchant/orders/counts", harness.LastRequest.Uri.ToString());
    }

    [Fact]
    public async Task Encodes_query_parameters()
    {
        using var harness = new Harness();
        harness.Handler.Enqueue(200, Fixtures.Read("orders-list.json"));

        await harness.Client.Orders.ListAsync(new ListOrdersRequest { Status = OrderStatusFilter.Captured, Search = "a b&c=+966", Page = 2, PageSize = 50 });

        Assert.Equal("?status=captured&search=a%20b%26c%3D%2B966&page=2&pageSize=50", harness.LastRequest.Uri.Query);
    }

    [Fact]
    public async Task Strips_control_characters_from_the_user_agent_suffix()
    {
        using var harness = new Harness(o => o.UserAgentSuffix = "shop/1.0\r\nX-Injected: yes");
        harness.Handler.Enqueue(200, Fixtures.Envelope("SUCCESS", new { all = 1 }));

        await harness.Client.Orders.CountsAsync();

        var userAgent = harness.LastRequest.Header("User-Agent")!;
        Assert.DoesNotContain("\n", userAgent, StringComparison.Ordinal);
        Assert.Null(harness.LastRequest.Header("X-Injected"));
        Assert.EndsWith("shop/1.0  X-Injected: yes", userAgent, StringComparison.Ordinal);
    }
}
