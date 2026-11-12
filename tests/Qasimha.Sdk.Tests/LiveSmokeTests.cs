using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using Qasimha.Sdk.Tests.Support;
using Xunit;

namespace Qasimha.Sdk.Tests;

public class LiveSmokeTests
{
    private const string TestEndpointUrl = "https://example.com/qasimha-sdk-test";

    [SkippableFact]
    public async Task The_cart_contract_holds_against_a_live_environment()
    {
        var apiKey = Environment.GetEnvironmentVariable("QASIMHA_API_KEY");
        var baseUrl = Environment.GetEnvironmentVariable("QASIMHA_BASE_URL");
        Skip.If(string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(baseUrl), "QASIMHA_API_KEY and QASIMHA_BASE_URL are not set.");

        using var handler = BuildHandler();
        using var client = new QasimhaClient(
            new QasimhaClientOptions { ApiKey = apiKey, BaseUrl = baseUrl, Timeout = TimeSpan.FromSeconds(60) },
            handler);

        var stamp = DateTime.UtcNow.ToString("HHmmss", System.Globalization.CultureInfo.InvariantCulture)
            + "-" + Random.Shared.Next(1000, 9999).ToString(System.Globalization.CultureInfo.InvariantCulture);

        var basket = Baskets.ThreeLine("CART-" + stamp + "-A");
        string orderId;
        try
        {
            var session = await client.Checkout.CreateSessionAsync(basket);
            Assert.StartsWith("https", session.PaymentUrl, StringComparison.Ordinal);
            Assert.False(string.IsNullOrEmpty(session.OrderId));
            orderId = session.OrderId;
        }
        catch (OrderDeclinedException declined)
        {
            Assert.False(string.IsNullOrEmpty(declined.OrderId));
            orderId = declined.OrderId!;
        }

        var order = await client.Orders.GetAsync(orderId);
        Assert.Equal(basket.OrderReferenceId, order.OrderReferenceId);
        Assert.Equal(3, order.Items.Count);
        var cable = Assert.Single(order.Items, i => i.Sku == "SKU-2");
        Assert.Equal(3, cable.Quantity);
        Assert.Contains(order.Items, i => i.Type == ItemType.Digital);
        Assert.Equal(1302.10m, order.TotalAmount);

        var byReference = await client.Orders.GetByReferenceAsync(basket.OrderReferenceId);
        Assert.Equal(orderId, byReference.Id);
        Assert.Equal(basket.OrderReferenceId, byReference.OrderReferenceId);
        Assert.Equal(order.Status, byReference.Status);
        Assert.Equal(3, byReference.Items.Count);
        Assert.Equal(1302.10m, byReference.TotalAmount);

        var unknown = await Assert.ThrowsAsync<NotFoundException>(() => client.Orders.GetByReferenceAsync("CART-" + stamp + "-NEVER"));
        Assert.Equal(404, unknown.HttpStatus);

        var page = await client.Orders.ListAsync(new ListOrdersRequest { Search = basket.OrderReferenceId, PageSize = 5 });
        Assert.Contains(page.Items, o => o.Id == orderId);
        Assert.True(page.Pagination.TotalItems >= 1);

        var counts = await client.Orders.CountsAsync();
        Assert.True(counts.All >= 1);

        var mismatch = Baskets.Single("CART-" + stamp + "-C", 10.00m, Baskets.Item("SKU-9", "Laptop", ItemType.Physical, 1, 5000.00m));
        var local = await Assert.ThrowsAsync<ValidationException>(() => client.Checkout.CreateSessionAsync(mismatch));
        Assert.Contains(local.Errors, e => e.Field == "TotalAmount");

        var foreignCurrency = Baskets.Single("CART-" + stamp + "-H", 250.00m, Baskets.Item("SKU-USD", "Item", ItemType.Physical, 1, 250.00m));
        foreignCurrency.CurrencyCode = "USD";
        var remote = await Assert.ThrowsAsync<ValidationException>(() => client.Checkout.CreateSessionAsync(foreignCurrency));
        Assert.Equal(400, remote.HttpStatus);

        var duplicate = await Assert.ThrowsAsync<DuplicateOrderReferenceException>(() => client.Checkout.CreateSessionAsync(basket));
        Assert.Equal(409, duplicate.HttpStatus);
        Assert.Equal(orderId, duplicate.ExistingOrderId);

        var arabic = Baskets.Single("CART-" + stamp + "-G", 250.00m, Baskets.Item("SKU-AR", "سماعات لاسلكية", ItemType.Physical, 1, 250.00m));
        string arabicOrderId;
        try
        {
            arabicOrderId = (await client.Checkout.CreateSessionAsync(arabic)).OrderId;
        }
        catch (OrderDeclinedException declined)
        {
            arabicOrderId = declined.OrderId!;
        }

        var arabicOrder = await client.Orders.GetAsync(arabicOrderId);
        Assert.Contains(arabicOrder.Items, i => i.Name == "سماعات لاسلكية");

        var arabicByReference = await client.Orders.GetByReferenceAsync(arabic.OrderReferenceId);
        Assert.Equal(arabicOrderId, arabicByReference.Id);
        Assert.Contains(arabicByReference.Items, i => i.Name == "سماعات لاسلكية");
    }

    [SkippableFact]
    public async Task Webhook_endpoints_round_trip_against_a_live_environment()
    {
        var apiKey = Environment.GetEnvironmentVariable("QASIMHA_WEBHOOKS_API_KEY");
        var baseUrl = Environment.GetEnvironmentVariable("QASIMHA_BASE_URL");
        Skip.If(string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(baseUrl), "QASIMHA_WEBHOOKS_API_KEY and QASIMHA_BASE_URL are not set.");

        using var handler = BuildHandler();
        using var client = new QasimhaClient(
            new QasimhaClientOptions { ApiKey = apiKey, BaseUrl = baseUrl, Timeout = TimeSpan.FromSeconds(60) },
            handler);
        var endpoints = client.WebhookEndpoints;

        foreach (var leftover in (await endpoints.ListAsync()).Items.Where(e => e.Url == TestEndpointUrl))
        {
            await endpoints.DeleteAsync(leftover.Id);
        }

        var created = await endpoints.CreateAsync(new CreateWebhookEndpointRequest
        {
            Url = TestEndpointUrl,
            Description = "SDK live smoke",
            Events = new[] { WebhookEventType.OrderConfirmed, WebhookEventType.OrderRefunded },
        });
        try
        {
            Assert.Equal(201, created.Response!.HttpStatus);
            Assert.StartsWith("whsec_", created.Secret, StringComparison.Ordinal);
            Assert.EndsWith(created.SecretHint!, created.Secret, StringComparison.Ordinal);
            Assert.True(created.IsActive);
            Assert.Equal(new[] { "order.confirmed", "order.refunded" }, created.Events);
            Assert.DoesNotContain(created.Secret!, created.ToString(), StringComparison.Ordinal);

            var fetched = await endpoints.GetAsync(created.Id);
            Assert.Equal(TestEndpointUrl, fetched.Url);
            Assert.Equal("SDK live smoke", fetched.Description);
            Assert.Equal(created.SecretHint, fetched.SecretHint);
            Assert.IsType<WebhookEndpoint>(fetched);

            var ping = await endpoints.PingAsync(created.Id);
            Assert.Equal(202, ping.Response!.HttpStatus);
            Assert.Equal(created.Id, ping.EndpointId);
            Assert.StartsWith("evt_", ping.EventId, StringComparison.Ordinal);

            WebhookDelivery? delivery = null;
            for (var attempt = 0; attempt < 30 && delivery is null; attempt++)
            {
                var log = await endpoints.ListDeliveriesAsync(created.Id, new ListWebhookDeliveriesRequest { PageSize = 10 });
                delivery = log.Items.FirstOrDefault(d => d.EventType == WebhookEventType.Ping);
                if (delivery is null)
                {
                    await Task.Delay(TimeSpan.FromSeconds(1));
                }
            }

            Assert.NotNull(delivery);
            Assert.True(delivery!.MaxAttempts >= 1);
            Assert.Contains(delivery.Status, new[] { WebhookDeliveryStatus.Pending, WebhookDeliveryStatus.Succeeded, WebhookDeliveryStatus.Failed });

            var replayed = await endpoints.ReplayDeliveryAsync(created.Id, delivery.Id);
            Assert.Equal(202, replayed.Response!.HttpStatus);
            Assert.Equal(delivery.Id, replayed.ReplayOfId);
            Assert.Equal(WebhookEventType.Ping, replayed.EventType);

            var paused = await endpoints.UpdateAsync(created.Id, new UpdateWebhookEndpointRequest { IsActive = false, Description = "SDK live smoke (paused)" });
            Assert.False(paused.IsActive);
            Assert.Equal("SDK live smoke (paused)", paused.Description);
            Assert.NotNull(paused.UpdatedAt);

            var rotated = await endpoints.RotateSecretAsync(created.Id);
            Assert.StartsWith("whsec_", rotated.Secret, StringComparison.Ordinal);
            Assert.NotEqual(created.Secret, rotated.Secret);
            Assert.EndsWith(rotated.SecretHint!, rotated.Secret, StringComparison.Ordinal);
            Assert.NotNull(rotated.PreviousSecretExpiresAt);
            Assert.False(rotated.IsActive);

            var listed = await endpoints.ListAsync();
            Assert.Contains(listed.Items, e => e.Id == created.Id && e.SecretHint == rotated.SecretHint && !e.IsActive);
        }
        finally
        {
            await endpoints.DeleteAsync(created.Id);
        }

        await Assert.ThrowsAsync<NotFoundException>(() => endpoints.GetAsync(created.Id));
        Assert.DoesNotContain((await endpoints.ListAsync()).Items, e => e.Id == created.Id);
    }

    [SkippableFact]
    public async Task Plans_are_quoted_against_a_live_environment()
    {
        var apiKey = Environment.GetEnvironmentVariable("QASIMHA_API_KEY");
        var publishableKey = Environment.GetEnvironmentVariable("QASIMHA_PUBLISHABLE_KEY");
        var baseUrl = Environment.GetEnvironmentVariable("QASIMHA_BASE_URL");
        Skip.If(
            string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(publishableKey) || string.IsNullOrWhiteSpace(baseUrl),
            "QASIMHA_API_KEY, QASIMHA_PUBLISHABLE_KEY and QASIMHA_BASE_URL are not set.");

        using var handler = BuildHandler();
        using var client = new QasimhaClient(
            new QasimhaClientOptions { ApiKey = apiKey, PublishableKey = publishableKey, BaseUrl = baseUrl, Timeout = TimeSpan.FromSeconds(60) },
            handler);

        var quote = await client.Checkout.GetPlansAsync(1200.00m);
        Assert.Equal(200, quote.Response!.HttpStatus);
        Assert.Equal("SAR", quote.Currency);
        Assert.False(string.IsNullOrEmpty(quote.MerchantName));
        Assert.True(quote.MinAmount <= 1200m && 1200m <= quote.MaxAmount);
        Assert.Equal(3, quote.Items.Count);
        Assert.All(quote.Items, plan =>
        {
            Assert.True(plan.Instalments >= 2);
            Assert.True(plan.InstalmentAmount > 0m);
            Assert.True(plan.TotalPayable >= 1200m);
            Assert.False(string.IsNullOrEmpty(plan.Frequency));
        });

        var arabic = await client.Checkout.GetPlansAsync(1200.00m, new GetPlansOptions { Lang = "ar" });
        Assert.Equal(quote.Items.Count, arabic.Items.Count);

        var tooSmall = await client.Checkout.GetPlansAsync(50.00m);
        Assert.True(50m < tooSmall.MinAmount);
        Assert.Empty(tooSmall.Items);

        var rejected = await Assert.ThrowsAsync<AuthenticationException>(() =>
            client.Checkout.GetPlansAsync(1200.00m, new GetPlansOptions { PublishableKey = "pk_test_doesnotexist" }));
        Assert.Equal(401, rejected.HttpStatus);
    }

    private static SocketsHttpHandler BuildHandler()
    {
        var handler = new SocketsHttpHandler { AllowAutoRedirect = false, UseCookies = false };

        var resolveTo = Environment.GetEnvironmentVariable("QASIMHA_RESOLVE_TO");
        if (!string.IsNullOrWhiteSpace(resolveTo) && IPAddress.TryParse(resolveTo, out var address))
        {
            handler.ConnectCallback = async (context, cancellationToken) =>
            {
                var socket = new Socket(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };
                try
                {
                    await socket.ConnectAsync(new IPEndPoint(address, context.DnsEndPoint.Port), cancellationToken);
                    return new NetworkStream(socket, ownsSocket: true);
                }
                catch
                {
                    socket.Dispose();
                    throw;
                }
            };
        }

        if (Environment.GetEnvironmentVariable("QASIMHA_TLS_INSECURE") == "1")
        {
            handler.SslOptions = new SslClientAuthenticationOptions
            {
                RemoteCertificateValidationCallback = (_, _, _, _) => true,
            };
        }

        return handler;
    }
}
