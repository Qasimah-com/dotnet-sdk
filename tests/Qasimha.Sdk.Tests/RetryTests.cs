using Qasimha.Sdk.Tests.Support;
using Xunit;

namespace Qasimha.Sdk.Tests;

public class RetryTests
{
    private static readonly string Counts = Fixtures.Envelope("SUCCESS", new { all = 1 });

    [Fact]
    public async Task Retries_a_503_with_exponential_backoff_and_jitter()
    {
        using var harness = new Harness();
        harness.Handler.Enqueue(503).Enqueue(503).Enqueue(200, Counts);

        var counts = await harness.Client.Orders.CountsAsync();

        Assert.Equal(1, counts.All);
        Assert.Equal(3, harness.Handler.Requests.Count);
        Assert.Equal(3, counts.Response!.Attempt);
        Assert.Collection(
            harness.Delay.Waits,
            w => Assert.InRange(w.TotalMilliseconds, 1000, 1250),
            w => Assert.InRange(w.TotalMilliseconds, 2000, 2250));
    }

    [Fact]
    public async Task Gives_up_after_max_retries()
    {
        using var harness = new Harness();
        harness.Handler.Enqueue(503);

        await Assert.ThrowsAsync<ServerException>(() => harness.Client.Orders.CountsAsync());

        Assert.Equal(3, harness.Handler.Requests.Count);
        Assert.Equal(2, harness.Delay.Waits.Count);
    }

    [Fact]
    public async Task Caps_the_backoff_at_eight_seconds()
    {
        using var harness = new Harness(o => o.MaxRetries = 5);
        harness.Handler.Enqueue(502);

        await Assert.ThrowsAsync<ServerException>(() => harness.Client.Orders.CountsAsync());

        Assert.Equal(6, harness.Handler.Requests.Count);
        Assert.Equal(5, harness.Delay.Waits.Count);
        Assert.InRange(harness.Delay.Waits[2].TotalMilliseconds, 4000, 4250);
        Assert.InRange(harness.Delay.Waits[3].TotalMilliseconds, 8000, 8250);
        Assert.InRange(harness.Delay.Waits[4].TotalMilliseconds, 8000, 8250);
    }

    [Fact]
    public async Task Honours_retry_after_seconds_on_a_429()
    {
        using var harness = new Harness();
        harness.Handler
            .Enqueue(429, null, r => r.Headers.Add("Retry-After", "7"))
            .Enqueue(200, Counts);

        await harness.Client.Orders.CountsAsync();

        Assert.Equal(new[] { TimeSpan.FromSeconds(7) }, harness.Delay.Waits);
    }

    [Fact]
    public async Task Caps_retry_after_at_thirty_seconds()
    {
        using var harness = new Harness();
        harness.Handler
            .Enqueue(429, null, r => r.Headers.Add("Retry-After", "600"))
            .Enqueue(200, Counts);

        await harness.Client.Orders.CountsAsync();

        Assert.Equal(new[] { TimeSpan.FromSeconds(30) }, harness.Delay.Waits);
    }

    [Fact]
    public async Task Honours_an_http_date_retry_after()
    {
        using var harness = new Harness();
        var date = harness.Now.AddSeconds(12).ToString("R", System.Globalization.CultureInfo.InvariantCulture);
        harness.Handler
            .Enqueue(429, null, r => r.Headers.Add("Retry-After", date))
            .Enqueue(200, Counts);

        await harness.Client.Orders.CountsAsync();

        Assert.Equal(new[] { TimeSpan.FromSeconds(12) }, harness.Delay.Waits);
    }

    [Fact]
    public async Task Falls_back_to_backoff_when_retry_after_is_in_the_past()
    {
        using var harness = new Harness();
        harness.Handler
            .Enqueue(429, null, r => r.Headers.Add("Retry-After", "0"))
            .Enqueue(200, Counts);

        await harness.Client.Orders.CountsAsync();

        Assert.InRange(harness.Delay.Waits.Single().TotalMilliseconds, 1000, 1250);
    }

    [Theory]
    [InlineData(502)]
    [InlineData(504)]
    public async Task Retries_gateway_errors(int status)
    {
        using var harness = new Harness();
        harness.Handler.Enqueue(status).Enqueue(200, Counts);

        await harness.Client.Orders.CountsAsync();

        Assert.Equal(2, harness.Handler.Requests.Count);
    }

    [Theory]
    [InlineData(400)]
    [InlineData(404)]
    [InlineData(409)]
    [InlineData(500)]
    public async Task Never_retries_other_statuses(int status)
    {
        using var harness = new Harness();
        harness.Handler.Enqueue(status, Fixtures.Envelope("X", null, success: false));

        await Assert.ThrowsAsync<QasimhaException>(async () =>
        {
            try
            {
                await harness.Client.Orders.CountsAsync();
            }
            catch (QasimhaException ex)
            {
                throw new QasimhaException(ex.Message, ex);
            }
        });

        Assert.Single(harness.Handler.Requests);
        Assert.Empty(harness.Delay.Waits);
    }

    [Fact]
    public async Task Retries_request_in_progress_then_gives_a_conflict()
    {
        using var harness = new Harness();
        harness.Handler.Enqueue(409, Fixtures.Envelope("REQUEST_IN_PROGRESS", null, success: false, message: "still running"));

        var ex = await Assert.ThrowsAsync<ConflictException>(() => harness.Client.Checkout.CreateSessionAsync(Baskets.ThreeLine()));

        Assert.Equal("REQUEST_IN_PROGRESS", ex.Code);
        Assert.Equal(3, harness.Handler.Requests.Count);
    }

    [Fact]
    public async Task Reuses_the_idempotency_key_and_correlation_id_across_attempts()
    {
        using var harness = new Harness();
        harness.Handler.Enqueue(503).EnqueueThrow(new HttpRequestException("reset")).Enqueue(201, Fixtures.SessionCreated);

        var session = await harness.Client.Checkout.CreateSessionAsync(Baskets.ThreeLine());

        Assert.Equal("9a2e6c4b-1111-4c2d-8a1f-000000000001", session.OrderId);
        var keys = harness.Handler.Requests.Select(r => r.Header("Idempotency-Key")).Distinct().ToList();
        var correlations = harness.Handler.Requests.Select(r => r.Header("X-Correlation-Id")).Distinct().ToList();
        Assert.Equal(3, harness.Handler.Requests.Count);
        Assert.Single(keys);
        Assert.Single(correlations);
        Assert.Equal(3, session.Response!.Attempt);
    }

    [Fact]
    public async Task Does_not_retry_a_post_when_idempotency_is_disabled()
    {
        using var harness = new Harness();
        harness.Handler.Enqueue(503);

        await Assert.ThrowsAsync<ServerException>(() =>
            harness.Client.Checkout.CreateSessionAsync(Baskets.ThreeLine(), new RequestOptions { Idempotent = false }));

        Assert.Single(harness.Handler.Requests);
    }

    [Fact]
    public async Task Does_not_retry_a_connection_error_on_a_post_when_idempotency_is_disabled()
    {
        using var harness = new Harness();
        harness.Handler.EnqueueThrow(new HttpRequestException("reset"));

        await Assert.ThrowsAsync<ConnectionException>(() =>
            harness.Client.Checkout.CreateSessionAsync(Baskets.ThreeLine(), new RequestOptions { Idempotent = false }));

        Assert.Single(harness.Handler.Requests);
    }

    [Fact]
    public async Task Retries_a_connection_error_on_a_get()
    {
        using var harness = new Harness();
        harness.Handler.EnqueueThrow(new IOException("reset")).Enqueue(200, Counts);

        await harness.Client.Orders.CountsAsync();

        Assert.Equal(2, harness.Handler.Requests.Count);
        Assert.InRange(harness.Delay.Waits.Single().TotalMilliseconds, 1000, 1250);
    }

    [Fact]
    public async Task Retries_a_per_attempt_timeout()
    {
        using var harness = new Harness(o =>
        {
            o.MaxRetries = 1;
            o.Timeout = TimeSpan.FromMilliseconds(50);
        });
        harness.Handler.EnqueueHang().Enqueue(200, Counts);

        await harness.Client.Orders.CountsAsync();

        Assert.Equal(2, harness.Handler.Requests.Count);
    }

    [Fact]
    public async Task Zero_retries_means_one_attempt()
    {
        using var harness = new Harness(o => o.MaxRetries = 0);
        harness.Handler.Enqueue(503);

        await Assert.ThrowsAsync<ServerException>(() => harness.Client.Orders.CountsAsync());

        Assert.Single(harness.Handler.Requests);
        Assert.Empty(harness.Delay.Waits);
    }
}
