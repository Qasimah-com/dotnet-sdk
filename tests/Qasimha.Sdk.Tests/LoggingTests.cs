using Qasimha.Sdk.Tests.Support;
using Xunit;

namespace Qasimha.Sdk.Tests;

public class LoggingTests
{
    private static readonly string[] ExpectedFields = { "method", "path", "status", "durationMs", "requestId", "attempt", "code" };

    [Fact]
    public async Task A_successful_call_logs_one_debug_entry_with_the_contract_fields()
    {
        using var harness = new Harness();
        harness.Handler.Enqueue(200, Fixtures.Read("orders-list.json"));

        await harness.Client.Orders.ListAsync();

        var entry = Assert.Single(harness.Logs);
        Assert.Equal(QasimhaLogLevel.Debug, entry.Level);
        Assert.Equal("request completed", entry.Message);
        foreach (var field in ExpectedFields)
        {
            Assert.True(entry.Fields.ContainsKey(field), field);
        }

        Assert.Equal("GET", entry.Fields["method"]);
        Assert.Equal("/api/v1/merchant/orders", entry.Fields["path"]);
        Assert.Equal(200, entry.Fields["status"]);
        Assert.Equal(1, entry.Fields["attempt"]);
        Assert.Equal("SUCCESS", entry.Fields["code"]);
        Assert.Equal("08bd4a65-79dd-46ee-8ee9-e6806313b740", entry.Fields["requestId"]);
        Assert.IsType<long>(entry.Fields["durationMs"]);
    }

    [Fact]
    public async Task The_query_string_never_reaches_the_log()
    {
        using var harness = new Harness();
        harness.Handler.Enqueue(200, Fixtures.Read("orders-list.json"));

        await harness.Client.Orders.ListAsync(new ListOrdersRequest { Search = "+966512349999" });

        Assert.DoesNotContain("966512349999", Flatten(harness.Logs), StringComparison.Ordinal);
        Assert.Equal("/api/v1/merchant/orders", Assert.Single(harness.Logs).Fields["path"]);
    }

    [Fact]
    public async Task Consumer_data_never_reaches_the_log()
    {
        using var harness = new Harness();
        harness.Handler.Enqueue(400, Fixtures.ValidationError);

        await Assert.ThrowsAsync<ValidationException>(() => harness.Client.Checkout.CreateSessionAsync(Baskets.ThreeLine()));

        var text = Flatten(harness.Logs);
        Assert.DoesNotContain("Noura", text, StringComparison.Ordinal);
        Assert.DoesNotContain("966512349999", text, StringComparison.Ordinal);
        Assert.DoesNotContain("shopper@example.com", text, StringComparison.Ordinal);
        Assert.DoesNotContain("King Fahd", text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Secrets_never_reach_the_log()
    {
        using var harness = new Harness();
        harness.Handler.Enqueue(503).EnqueueThrow(new HttpRequestException("reset")).Enqueue(401);

        await Assert.ThrowsAsync<AuthenticationException>(() => harness.Client.Orders.CountsAsync());

        var text = Flatten(harness.Logs);
        Assert.DoesNotContain(Harness.ApiKey, text, StringComparison.Ordinal);
        Assert.DoesNotContain(Harness.WebhookSecret, text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Retries_log_a_warning_then_the_outcome()
    {
        using var harness = new Harness();
        harness.Handler.Enqueue(503).Enqueue(200, Fixtures.Envelope("SUCCESS", new { all = 1 }));

        await harness.Client.Orders.CountsAsync();

        Assert.Collection(
            harness.Logs,
            e =>
            {
                Assert.Equal(QasimhaLogLevel.Warn, e.Level);
                Assert.Equal("request failed, retrying", e.Message);
                Assert.Equal(503, e.Fields["status"]);
                Assert.Equal(1, e.Fields["attempt"]);
            },
            e =>
            {
                Assert.Equal(QasimhaLogLevel.Debug, e.Level);
                Assert.Equal(2, e.Fields["attempt"]);
            });
    }

    [Fact]
    public async Task A_final_failure_logs_an_error()
    {
        using var harness = new Harness(o => o.MaxRetries = 0);
        harness.Handler.EnqueueThrow(new HttpRequestException("down"));

        await Assert.ThrowsAsync<ConnectionException>(() => harness.Client.Orders.CountsAsync());

        var entry = Assert.Single(harness.Logs);
        Assert.Equal(QasimhaLogLevel.Error, entry.Level);
        Assert.Equal("request failed", entry.Message);
        Assert.Null(entry.Fields["status"]);
        Assert.Null(entry.Fields["code"]);
    }

    [Fact]
    public async Task No_logger_means_no_logging()
    {
        var handler = new FakeHandler().Enqueue(200, Fixtures.Envelope("SUCCESS", new { all = 1 }));
        using var client = new QasimhaClient(new QasimhaClientOptions { ApiKey = "sk_test_abc", Delay = new FakeDelay() }, handler);

        var counts = await client.Orders.CountsAsync();

        Assert.Equal(1, counts.All);
    }

    private static string Flatten(IEnumerable<LogEntry> entries) =>
        string.Join("\n", entries.Select(e => e.Message + " " + string.Join(" ", e.Fields.Select(f => f.Key + "=" + f.Value))));
}
