using Qasimha.Sdk.Tests.Support;
using Xunit;

namespace Qasimha.Sdk.Tests;

public class WebhookEndpointsTests
{
    private const string EndpointId = "a81cce0b-35c7-45e0-a212-0c9f0969ddf4";

    private const string DeliveryId = "6e44f7d4-a878-4e1c-9148-e7f8745fb50d";

    [Fact]
    public async Task Creates_an_endpoint_from_the_recorded_fixture()
    {
        using var harness = new Harness();
        harness.Handler.Enqueue(201, Fixtures.Read("webhook-endpoint-create.json"));

        var endpoint = await harness.Client.WebhookEndpoints.CreateAsync(new CreateWebhookEndpointRequest
        {
            Url = "https://example.com/qasimha-webhook",
            Description = "SDK fixture endpoint",
            Events = new[] { WebhookEventType.OrderConfirmed, WebhookEventType.OrderRefunded },
        });

        Assert.Equal(HttpMethod.Post, harness.LastRequest.Method);
        Assert.Equal("/api/v1/merchant/webhooks", harness.LastRequest.Uri.AbsolutePath);
        Assert.Equal(
            "{\"url\":\"https://example.com/qasimha-webhook\",\"description\":\"SDK fixture endpoint\",\"events\":[\"order.confirmed\",\"order.refunded\"]}",
            harness.LastRequest.Body);
        Assert.NotNull(harness.LastRequest.Header("Idempotency-Key"));

        Assert.Equal(EndpointId, endpoint.Id);
        Assert.Equal("https://example.com/qasimha-webhook", endpoint.Url);
        Assert.Equal("SDK fixture endpoint", endpoint.Description);
        Assert.Equal(new[] { "order.confirmed", "order.refunded" }, endpoint.Events);
        Assert.True(endpoint.IsActive);
        Assert.Equal("whsec_REDACTED_b877", endpoint.Secret);
        Assert.Equal("b877", endpoint.SecretHint);
        Assert.Null(endpoint.PreviousSecretExpiresAt);
        Assert.Equal(new DateTimeOffset(2026, 9, 25, 23, 42, 29, TimeSpan.Zero).AddTicks(5562693), endpoint.CreatedAt);
        Assert.Null(endpoint.UpdatedAt);
        Assert.Equal(201, endpoint.Response!.HttpStatus);
        Assert.Equal("9b2016cc-4c8a-453a-9185-46220744cb18", endpoint.RequestId);
    }

    [Fact]
    public async Task Omits_unset_members_from_the_create_body()
    {
        using var harness = new Harness();
        harness.Handler.Enqueue(201, Fixtures.Read("webhook-endpoint-create.json"));

        await harness.Client.WebhookEndpoints.CreateAsync(new CreateWebhookEndpointRequest { Url = "https://example.com/hook" });

        Assert.Equal("{\"url\":\"https://example.com/hook\"}", harness.LastRequest.Body);
    }

    [Fact]
    public async Task The_secret_is_neither_logged_nor_part_of_to_string()
    {
        using var harness = new Harness();
        harness.Handler.Enqueue(201, Fixtures.Read("webhook-endpoint-create.json")).Enqueue(200, Fixtures.Read("webhook-endpoint-rotate-secret.json"));

        var created = await harness.Client.WebhookEndpoints.CreateAsync(new CreateWebhookEndpointRequest { Url = "https://example.com/hook" });
        var rotated = await harness.Client.WebhookEndpoints.RotateSecretAsync(created.Id);

        foreach (var text in new[] { created.ToString(), rotated.ToString(), Flatten(harness.Logs) })
        {
            Assert.DoesNotContain("whsec_", text, StringComparison.Ordinal);
            Assert.DoesNotContain("REDACTED", text, StringComparison.Ordinal);
        }

        Assert.Equal("WebhookEndpoint(" + EndpointId + ", …b877)", created.ToString());
        Assert.Equal("WebhookEndpoint(" + EndpointId + ", …be3e)", rotated.ToString());
        Assert.Equal(2, harness.Logs.Count);
    }

    [Theory]
    [InlineData("http://example.com/hook")]
    [InlineData("example.com/hook")]
    [InlineData("ftp://example.com/hook")]
    [InlineData("https://")]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Refuses_a_url_that_is_not_https(string url)
    {
        using var harness = new Harness();

        var ex = await Assert.ThrowsAsync<ValidationException>(() => harness.Client.WebhookEndpoints.CreateAsync(new CreateWebhookEndpointRequest { Url = url }));

        var error = Assert.Single(ex.Errors);
        Assert.Equal("Url", error.Field);
        Assert.Empty(harness.Handler.Requests);
    }

    [Fact]
    public async Task Refuses_an_unknown_event_name()
    {
        using var harness = new Harness();

        var ex = await Assert.ThrowsAsync<ValidationException>(() => harness.Client.WebhookEndpoints.CreateAsync(new CreateWebhookEndpointRequest
        {
            Url = "https://example.com/hook",
            Events = new[] { WebhookEventType.OrderConfirmed, "order.reversed", "ping" },
        }));

        Assert.Equal(new[] { "Events[1]", "Events[2]" }, ex.Errors.Select(e => e.Field));
        Assert.Contains("order.confirmed", ex.Errors[0].Message, StringComparison.Ordinal);
        Assert.Empty(harness.Handler.Requests);
    }

    [Fact]
    public async Task Accepts_every_documented_event_name()
    {
        using var harness = new Harness();
        harness.Handler.Enqueue(201, Fixtures.Read("webhook-endpoint-create.json"));

        await harness.Client.WebhookEndpoints.CreateAsync(new CreateWebhookEndpointRequest
        {
            Url = "https://example.com/hook",
            Events = new[]
            {
                WebhookEventType.OrderConfirmed, WebhookEventType.OrderDeclined, WebhookEventType.OrderFulfilled,
                WebhookEventType.OrderPartiallyFulfilled, WebhookEventType.OrderRefunded, WebhookEventType.OrderPartiallyRefunded,
                WebhookEventType.OrderVoided, WebhookEventType.OrderCancelled, WebhookEventType.OrderExpired,
                WebhookEventType.CheckoutLinkExpired, WebhookEventType.OrderCompleted,
            },
        });

        Assert.Single(harness.Handler.Requests);
    }

    [Fact]
    public async Task Api_create_errors_keep_their_code()
    {
        using var harness = new Harness();
        harness.Handler.Enqueue(400, Fixtures.Envelope("LIMIT_EXCEEDED", null, success: false, message: "At most 5 endpoints"));

        var ex = await Assert.ThrowsAsync<ValidationException>(() => harness.Client.WebhookEndpoints.CreateAsync(new CreateWebhookEndpointRequest { Url = "https://example.com/hook" }));

        Assert.Equal("LIMIT_EXCEEDED", ex.Code);
        Assert.Equal(400, ex.HttpStatus);
    }

    [Fact]
    public async Task Lists_endpoints_from_the_recorded_fixture()
    {
        using var harness = new Harness();
        harness.Handler.Enqueue(200, Fixtures.Read("webhook-endpoint-list.json"));

        var list = await harness.Client.WebhookEndpoints.ListAsync();

        Assert.Equal(HttpMethod.Get, harness.LastRequest.Method);
        Assert.Equal("/api/v1/merchant/webhooks", harness.LastRequest.Uri.AbsolutePath);
        var endpoint = Assert.Single(list.Items);
        Assert.Equal(EndpointId, endpoint.Id);
        Assert.Equal("https://example.com/qasimha-webhook", endpoint.Url);
        Assert.Equal(2, endpoint.Events.Count);
        Assert.True(endpoint.IsActive);
        Assert.Equal("b877", endpoint.SecretHint);
        Assert.IsType<WebhookEndpoint>(endpoint);
        Assert.Equal("114c13ad-5036-444f-85af-0f8cf8d9fe61", list.RequestId);
    }

    [Fact]
    public async Task Reads_an_endpoint_from_the_recorded_fixture()
    {
        using var harness = new Harness();
        harness.Handler.Enqueue(200, Fixtures.Read("webhook-endpoint-get.json"));

        var endpoint = await harness.Client.WebhookEndpoints.GetAsync(EndpointId);

        Assert.Equal("/api/v1/merchant/webhooks/" + EndpointId, harness.LastRequest.Uri.AbsolutePath);
        Assert.Equal(EndpointId, endpoint.Id);
        Assert.Equal("SDK fixture endpoint", endpoint.Description);
        Assert.Equal(new DateTimeOffset(2026, 9, 25, 23, 42, 29, TimeSpan.Zero).AddTicks(5562690), endpoint.CreatedAt);
        Assert.Null(endpoint.UpdatedAt);
        Assert.Null(endpoint.PreviousSecretExpiresAt);
    }

    [Fact]
    public async Task Updates_an_endpoint_from_the_recorded_fixture()
    {
        using var harness = new Harness();
        harness.Handler.Enqueue(200, Fixtures.Read("webhook-endpoint-update.json"));

        var endpoint = await harness.Client.WebhookEndpoints.UpdateAsync(EndpointId, new UpdateWebhookEndpointRequest
        {
            Description = "SDK fixture endpoint (paused)",
            IsActive = false,
        });

        Assert.Equal(HttpMethod.Put, harness.LastRequest.Method);
        Assert.Equal("/api/v1/merchant/webhooks/" + EndpointId, harness.LastRequest.Uri.AbsolutePath);
        Assert.Equal("{\"description\":\"SDK fixture endpoint (paused)\",\"isActive\":false}", harness.LastRequest.Body);
        Assert.Null(harness.LastRequest.Header("Idempotency-Key"));
        Assert.False(endpoint.IsActive);
        Assert.Equal("SDK fixture endpoint (paused)", endpoint.Description);
        Assert.Equal(new DateTimeOffset(2026, 9, 25, 23, 42, 37, TimeSpan.Zero).AddTicks(3003779), endpoint.UpdatedAt);
    }

    [Fact]
    public async Task An_empty_event_list_is_sent_as_every_event()
    {
        using var harness = new Harness();
        harness.Handler.Enqueue(200, Fixtures.Read("webhook-endpoint-update.json"));

        await harness.Client.WebhookEndpoints.UpdateAsync(EndpointId, new UpdateWebhookEndpointRequest { Events = Array.Empty<string>() });

        Assert.Equal("{\"events\":[]}", harness.LastRequest.Body);
    }

    [Fact]
    public async Task Update_checks_the_url_and_the_event_names()
    {
        using var harness = new Harness();

        var badUrl = await Assert.ThrowsAsync<ValidationException>(() => harness.Client.WebhookEndpoints.UpdateAsync(EndpointId, new UpdateWebhookEndpointRequest { Url = "http://example.com/hook" }));
        Assert.Equal("Url", Assert.Single(badUrl.Errors).Field);

        var emptyUrl = await Assert.ThrowsAsync<ValidationException>(() => harness.Client.WebhookEndpoints.UpdateAsync(EndpointId, new UpdateWebhookEndpointRequest { Url = "" }));
        Assert.Equal("Url", Assert.Single(emptyUrl.Errors).Field);

        var badEvent = await Assert.ThrowsAsync<ValidationException>(() => harness.Client.WebhookEndpoints.UpdateAsync(EndpointId, new UpdateWebhookEndpointRequest { Events = new[] { "order.nope" } }));
        Assert.Equal("Events[0]", Assert.Single(badEvent.Errors).Field);

        Assert.Empty(harness.Handler.Requests);
    }

    [Fact]
    public async Task Deletes_an_endpoint_from_the_recorded_fixture()
    {
        using var harness = new Harness();
        harness.Handler.Enqueue(200, Fixtures.Read("webhook-endpoint-delete.json"));

        await harness.Client.WebhookEndpoints.DeleteAsync(EndpointId);

        Assert.Equal(HttpMethod.Delete, harness.LastRequest.Method);
        Assert.Equal("/api/v1/merchant/webhooks/" + EndpointId, harness.LastRequest.Uri.AbsolutePath);
        Assert.Null(harness.LastRequest.Body);
        Assert.Null(harness.LastRequest.Header("Idempotency-Key"));
    }

    [Fact]
    public async Task Update_and_delete_are_sent_once()
    {
        using var harness = new Harness();
        harness.Handler.Enqueue(503);

        await Assert.ThrowsAsync<ServerException>(() => harness.Client.WebhookEndpoints.UpdateAsync(EndpointId, new UpdateWebhookEndpointRequest { IsActive = true }));
        await Assert.ThrowsAsync<ServerException>(() => harness.Client.WebhookEndpoints.DeleteAsync(EndpointId));

        Assert.Equal(2, harness.Handler.Requests.Count);
        Assert.Empty(harness.Delay.Waits);
    }

    [Fact]
    public async Task Rotates_the_secret_from_the_recorded_fixture()
    {
        using var harness = new Harness();
        harness.Handler.Enqueue(200, Fixtures.Read("webhook-endpoint-rotate-secret.json"));

        var endpoint = await harness.Client.WebhookEndpoints.RotateSecretAsync(EndpointId);

        Assert.Equal(HttpMethod.Post, harness.LastRequest.Method);
        Assert.Equal("/api/v1/merchant/webhooks/" + EndpointId + "/rotate-secret", harness.LastRequest.Uri.AbsolutePath);
        Assert.Null(harness.LastRequest.Body);
        Assert.NotNull(harness.LastRequest.Header("Idempotency-Key"));
        Assert.Equal("whsec_REDACTED_be3e", endpoint.Secret);
        Assert.Equal("be3e", endpoint.SecretHint);
        Assert.False(endpoint.IsActive);
        Assert.Equal(new DateTimeOffset(2026, 9, 26, 23, 42, 37, TimeSpan.Zero).AddTicks(8012856), endpoint.PreviousSecretExpiresAt);
    }

    [Fact]
    public async Task A_rotation_retried_after_a_503_reuses_its_idempotency_key()
    {
        using var harness = new Harness();
        harness.Handler.Enqueue(503).Enqueue(200, Fixtures.Read("webhook-endpoint-rotate-secret.json"));

        var endpoint = await harness.Client.WebhookEndpoints.RotateSecretAsync(EndpointId);

        Assert.Equal(2, harness.Handler.Requests.Count);
        Assert.Equal(harness.Handler.Requests[0].Header("Idempotency-Key"), harness.Handler.Requests[1].Header("Idempotency-Key"));
        Assert.Equal("be3e", endpoint.SecretHint);
    }

    [Fact]
    public async Task Pings_an_endpoint_from_the_recorded_fixture()
    {
        using var harness = new Harness();
        harness.Handler.Enqueue(202, Fixtures.Read("webhook-endpoint-ping.json"));

        var ping = await harness.Client.WebhookEndpoints.PingAsync(EndpointId);

        Assert.Equal(HttpMethod.Post, harness.LastRequest.Method);
        Assert.Equal("/api/v1/merchant/webhooks/" + EndpointId + "/ping", harness.LastRequest.Uri.AbsolutePath);
        Assert.Equal("evt_a05fd3fddfea4ac8bf76a442e3cf441d", ping.EventId);
        Assert.Equal(EndpointId, ping.EndpointId);
        Assert.Equal(202, ping.Response!.HttpStatus);
    }

    [Fact]
    public async Task Lists_deliveries_from_the_recorded_fixture()
    {
        using var harness = new Harness();
        harness.Handler.Enqueue(200, Fixtures.Read("webhook-endpoint-deliveries.json"));

        var page = await harness.Client.WebhookEndpoints.ListDeliveriesAsync(EndpointId, new ListWebhookDeliveriesRequest
        {
            Status = WebhookDeliveryStatus.Pending,
            Page = 1,
            PageSize = 5,
        });

        Assert.Equal(HttpMethod.Get, harness.LastRequest.Method);
        Assert.Equal("/api/v1/merchant/webhooks/" + EndpointId + "/deliveries", harness.LastRequest.Uri.AbsolutePath);
        Assert.Equal("?status=Pending&page=1&pageSize=5", harness.LastRequest.Uri.Query);

        var delivery = Assert.Single(page.Items);
        Assert.Equal(DeliveryId, delivery.Id);
        Assert.Equal("a05fd3fd-dfea-4ac8-bf76-a442e3cf441d", delivery.EventId);
        Assert.Equal(WebhookEventType.Ping, delivery.EventType);
        Assert.Null(delivery.OrderId);
        Assert.Null(delivery.OrderReferenceId);
        Assert.Equal(WebhookDeliveryStatus.Pending, delivery.Status);
        Assert.Equal(1, delivery.Attempts);
        Assert.Equal(11, delivery.MaxAttempts);
        Assert.Equal(new DateTimeOffset(2026, 9, 25, 23, 43, 33, TimeSpan.Zero).AddTicks(3802570), delivery.NextAttemptAt);
        Assert.Equal(new DateTimeOffset(2026, 9, 25, 23, 42, 33, TimeSpan.Zero).AddTicks(3802570), delivery.LastAttemptAt);
        Assert.Equal(405, delivery.LastStatusCode);
        Assert.Equal("HTTP 405", delivery.LastError);
        Assert.StartsWith("<!doctype html>", delivery.ResponseSnippet, StringComparison.Ordinal);
        Assert.Equal(759, delivery.DurationMs);
        Assert.Null(delivery.SucceededAt);
        Assert.Null(delivery.ReplayOfId);

        Assert.Equal(1, page.Pagination.TotalItems);
        Assert.Equal(1, page.Pagination.Page);
        Assert.Equal(5, page.Pagination.PageSize);
        Assert.Equal(1, page.Pagination.TotalPages);
    }

    [Fact]
    public async Task Lists_deliveries_without_filters()
    {
        using var harness = new Harness();
        harness.Handler.Enqueue(200, Fixtures.Read("webhook-endpoint-deliveries.json"));

        await harness.Client.WebhookEndpoints.ListDeliveriesAsync(EndpointId);

        Assert.Equal(string.Empty, harness.LastRequest.Uri.Query);
    }

    [Fact]
    public async Task Replays_a_delivery_from_the_recorded_fixture()
    {
        using var harness = new Harness();
        harness.Handler.Enqueue(202, Fixtures.Read("webhook-endpoint-replay.json"));

        var replay = await harness.Client.WebhookEndpoints.ReplayDeliveryAsync(EndpointId, DeliveryId);

        Assert.Equal(HttpMethod.Post, harness.LastRequest.Method);
        Assert.Equal("/api/v1/merchant/webhooks/" + EndpointId + "/deliveries/" + DeliveryId + "/replay", harness.LastRequest.Uri.AbsolutePath);
        Assert.NotNull(harness.LastRequest.Header("Idempotency-Key"));
        Assert.Equal("2743654e-550b-458d-bfe2-ad3338d47807", replay.Id);
        Assert.Equal(DeliveryId, replay.ReplayOfId);
        Assert.Equal(WebhookDeliveryStatus.Pending, replay.Status);
        Assert.Equal(0, replay.Attempts);
        Assert.Null(replay.LastAttemptAt);
        Assert.Null(replay.LastStatusCode);
        Assert.Null(replay.DurationMs);
        Assert.Equal(202, replay.Response!.HttpStatus);
    }

    [Fact]
    public async Task A_key_without_the_scope_is_refused()
    {
        using var harness = new Harness();
        harness.Handler.Enqueue(403);

        var client = harness.Client.WebhookEndpoints;
        await Assert.ThrowsAsync<PermissionException>(() => client.CreateAsync(new CreateWebhookEndpointRequest { Url = "https://example.com/hook" }));
        await Assert.ThrowsAsync<PermissionException>(() => client.ListAsync());
        await Assert.ThrowsAsync<PermissionException>(() => client.GetAsync(EndpointId));
        await Assert.ThrowsAsync<PermissionException>(() => client.UpdateAsync(EndpointId, new UpdateWebhookEndpointRequest { IsActive = false }));
        await Assert.ThrowsAsync<PermissionException>(() => client.DeleteAsync(EndpointId));
        await Assert.ThrowsAsync<PermissionException>(() => client.RotateSecretAsync(EndpointId));
        await Assert.ThrowsAsync<PermissionException>(() => client.PingAsync(EndpointId));
        await Assert.ThrowsAsync<PermissionException>(() => client.ListDeliveriesAsync(EndpointId));
        await Assert.ThrowsAsync<PermissionException>(() => client.ReplayDeliveryAsync(EndpointId, DeliveryId));

        Assert.Equal(9, harness.Handler.Requests.Count);
    }

    [Fact]
    public async Task A_missing_endpoint_is_not_found()
    {
        using var harness = new Harness();
        harness.Handler.Enqueue(404, Fixtures.Envelope("NOT_FOUND", null, success: false, message: "Webhook endpoint not found"));

        var client = harness.Client.WebhookEndpoints;
        var ex = await Assert.ThrowsAsync<NotFoundException>(() => client.GetAsync(EndpointId));
        Assert.Equal("NOT_FOUND", ex.Code);
        await Assert.ThrowsAsync<NotFoundException>(() => client.UpdateAsync(EndpointId, new UpdateWebhookEndpointRequest { IsActive = false }));
        await Assert.ThrowsAsync<NotFoundException>(() => client.DeleteAsync(EndpointId));
        await Assert.ThrowsAsync<NotFoundException>(() => client.RotateSecretAsync(EndpointId));
        await Assert.ThrowsAsync<NotFoundException>(() => client.PingAsync(EndpointId));
        await Assert.ThrowsAsync<NotFoundException>(() => client.ListDeliveriesAsync(EndpointId));
        await Assert.ThrowsAsync<NotFoundException>(() => client.ReplayDeliveryAsync(EndpointId, DeliveryId));
    }

    [Fact]
    public async Task Escapes_ids_in_the_path()
    {
        using var harness = new Harness();
        harness.Handler.Enqueue(404, Fixtures.Envelope("NOT_FOUND", null, success: false));

        await Assert.ThrowsAsync<NotFoundException>(() => harness.Client.WebhookEndpoints.GetAsync("a b/c"));
        Assert.Equal("/api/v1/merchant/webhooks/a%20b%2Fc", harness.LastRequest.Uri.AbsolutePath);

        await Assert.ThrowsAsync<NotFoundException>(() => harness.Client.WebhookEndpoints.ReplayDeliveryAsync("e 1", "d/2"));
        Assert.Equal("/api/v1/merchant/webhooks/e%201/deliveries/d%2F2/replay", harness.LastRequest.Uri.AbsolutePath);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public async Task Refuses_an_empty_id(string id)
    {
        using var harness = new Harness();
        var client = harness.Client.WebhookEndpoints;

        await Assert.ThrowsAsync<ArgumentException>(() => client.GetAsync(id));
        await Assert.ThrowsAsync<ArgumentException>(() => client.UpdateAsync(id, new UpdateWebhookEndpointRequest()));
        await Assert.ThrowsAsync<ArgumentException>(() => client.DeleteAsync(id));
        await Assert.ThrowsAsync<ArgumentException>(() => client.RotateSecretAsync(id));
        await Assert.ThrowsAsync<ArgumentException>(() => client.PingAsync(id));
        await Assert.ThrowsAsync<ArgumentException>(() => client.ListDeliveriesAsync(id));
        await Assert.ThrowsAsync<ArgumentException>(() => client.ReplayDeliveryAsync(id, DeliveryId));
        await Assert.ThrowsAsync<ArgumentException>(() => client.ReplayDeliveryAsync(EndpointId, id));
        await Assert.ThrowsAsync<ArgumentNullException>(() => client.CreateAsync(null!));
        await Assert.ThrowsAsync<ArgumentNullException>(() => client.UpdateAsync(EndpointId, null!));
        Assert.Empty(harness.Handler.Requests);
    }

    private static string Flatten(IEnumerable<LogEntry> entries) =>
        string.Join("\n", entries.Select(e => e.Message + " " + string.Join(" ", e.Fields.Select(f => f.Key + "=" + f.Value))));
}
