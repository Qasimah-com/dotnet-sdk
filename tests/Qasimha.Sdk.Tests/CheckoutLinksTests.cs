using Qasimha.Sdk.Tests.Support;
using Xunit;

namespace Qasimha.Sdk.Tests;

public class CheckoutLinksTests
{
    private const string Created = """
        {"success":true,"uuid":"55555555-5555-4555-8555-555555555555","code":"LINK_CREATED","message":"Link created",
         "data":{"id":"l-1","url":"https://localhost/pay/l-1","channel":"Link","amount":250.00,"currency":"SAR","phone":"+966512349999","language":"ar",
                 "orderReferenceId":"ORDER-12345","ejarContractNumber":null,
                 "items":[{"name":"Headphones","sku":"SKU-1","quantity":1,"unitPrice":250.00,"total":250.00,"imageUrl":null}],
                 "status":"AwaitingCheckout","expiresAt":"2026-09-25T11:00:00Z"},
         "errors":null}
        """;

    [Fact]
    public async Task Creates_a_link()
    {
        using var harness = new Harness();
        harness.Handler.Enqueue(201, Created);

        var link = await harness.Client.CheckoutLinks.CreateAsync(new CreateCheckoutLinkRequest
        {
            Amount = 250.00m,
            Phone = "+966512349999",
            Language = "ar",
            OrderReferenceId = "ORDER-12345",
            Items = new[] { new CheckoutLinkItem { Name = "Headphones", Sku = "SKU-1", UnitPrice = 250.00m } },
        });

        Assert.Equal("/api/v1/merchant/checkout-links", harness.LastRequest.Uri.AbsolutePath);
        Assert.Equal(
            "{\"amount\":250.00,\"channel\":\"Link\",\"phone\":\"+966512349999\",\"language\":\"ar\",\"orderReferenceId\":\"ORDER-12345\",\"items\":[{\"name\":\"Headphones\",\"sku\":\"SKU-1\",\"quantity\":1,\"unitPrice\":250.00}]}",
            harness.LastRequest.Body);
        Assert.Equal("l-1", link.Id);
        Assert.Equal("https://localhost/pay/l-1", link.Url);
        Assert.Equal(CheckoutLinkChannel.Link, link.Channel);
        Assert.Equal(250.00m, link.Amount);
        Assert.Equal("SAR", link.Currency);
        Assert.Equal("ar", link.Language);
        Assert.Equal(OrderStatus.AwaitingCheckout, link.Status);
        Assert.Equal(new DateTimeOffset(2026, 9, 25, 11, 0, 0, TimeSpan.Zero), link.ExpiresAt);
        var line = Assert.Single(link.Items);
        Assert.Equal(250.00m, line.Total);
        Assert.Null(link.Completed);
    }

    [Fact]
    public async Task A_link_needs_a_phone()
    {
        using var harness = new Harness();

        var ex = await Assert.ThrowsAsync<ValidationException>(() => harness.Client.CheckoutLinks.CreateAsync(new CreateCheckoutLinkRequest { Amount = 10m }));

        var error = Assert.Single(ex.Errors);
        Assert.Equal("Phone", error.Field);
        Assert.Empty(harness.Handler.Requests);
    }

    [Fact]
    public async Task A_qr_link_needs_no_phone()
    {
        using var harness = new Harness();
        harness.Handler.Enqueue(201, Created);

        await harness.Client.CheckoutLinks.CreateAsync(new CreateCheckoutLinkRequest { Amount = 10m, Channel = CheckoutLinkChannel.Qr });

        Assert.Equal("{\"amount\":10,\"channel\":\"Qr\"}", harness.LastRequest.Body);
    }

    [Theory]
    [InlineData("Sms")]
    [InlineData("")]
    public async Task Refuses_an_unknown_channel(string channel)
    {
        using var harness = new Harness();

        var ex = await Assert.ThrowsAsync<ValidationException>(() => harness.Client.CheckoutLinks.CreateAsync(new CreateCheckoutLinkRequest { Amount = 10m, Channel = channel, Phone = "+9665" }));

        Assert.Contains(ex.Errors, e => e.Field == "Channel");
    }

    [Fact]
    public async Task Api_create_errors_keep_their_code()
    {
        using var harness = new Harness();
        harness.Handler.Enqueue(400, Fixtures.Envelope("EJAR_CONTRACT_NUMBER_REQUIRED", null, success: false, message: "Ejar contract required"));

        var ex = await Assert.ThrowsAsync<ValidationException>(() => harness.Client.CheckoutLinks.CreateAsync(new CreateCheckoutLinkRequest { Amount = 18000m, Phone = "+9665" }));

        Assert.Equal("EJAR_CONTRACT_NUMBER_REQUIRED", ex.Code);
    }

    [Fact]
    public async Task Lists_links_under_data_items()
    {
        using var harness = new Harness();
        harness.Handler.Enqueue(200, Fixtures.Envelope("SUCCESS", new
        {
            items = new object[]
            {
                new { id = "l-1", orderReferenceId = "ORDER-1", amount = 250.00, currency = "SAR", phone = "+9665****999", channel = "Link", status = "AwaitingCheckout", waiting = true },
                new { id = "l-2", orderReferenceId = "ORDER-2", amount = 99.00, currency = "SAR", phone = "+9665****998", channel = "Qr", status = "Expired", waiting = false },
            },
        }));

        var list = await harness.Client.CheckoutLinks.ListAsync();

        Assert.Equal("/api/v1/merchant/checkout-links", harness.LastRequest.Uri.AbsolutePath);
        Assert.Equal(2, list.Items.Count);
        Assert.True(list.Items[0].Waiting);
        Assert.Equal("+9665****999", list.Items[0].Phone);
        Assert.Equal(OrderStatus.Expired, list.Items[1].Status);
        Assert.Null(list.Items[1].Url);
    }

    [Fact]
    public async Task Lists_links_from_a_bare_array()
    {
        using var harness = new Harness();
        harness.Handler.Enqueue(200, Fixtures.Envelope("SUCCESS", new[] { new { id = "l-1" } }));

        var list = await harness.Client.CheckoutLinks.ListAsync();

        Assert.Equal("l-1", Assert.Single(list.Items).Id);
    }

    [Fact]
    public async Task Reads_a_link()
    {
        using var harness = new Harness();
        harness.Handler.Enqueue(200, Fixtures.Envelope("SUCCESS", new
        {
            id = "l-1",
            orderReferenceId = "ORDER-1",
            ejarContractNumber = (string?)null,
            amount = 250.00,
            currency = "SAR",
            phone = "+9665****999",
            channel = "Link",
            status = 5,
            decisionStatus = "APPROVED",
            expiresAt = "2026-09-25T11:00:00Z",
            createdAt = "2026-09-25T09:00:00Z",
            completed = true,
            declined = false,
        }));

        var link = await harness.Client.CheckoutLinks.GetAsync("l-1");

        Assert.Equal("/api/v1/merchant/checkout-links/l-1", harness.LastRequest.Uri.AbsolutePath);
        Assert.Equal(OrderStatus.Confirmed, link.Status);
        Assert.Equal("APPROVED", link.DecisionStatus);
        Assert.True(link.Completed);
        Assert.False(link.Declined);
        Assert.Equal(new DateTimeOffset(2026, 9, 25, 9, 0, 0, TimeSpan.Zero), link.CreatedAt);
    }

    [Fact]
    public async Task Fills_the_id_and_url_from_alternative_names()
    {
        using var harness = new Harness();
        harness.Handler.Enqueue(201, Fixtures.Envelope("LINK_CREATED", new { installmentId = "l-9", paymentUrl = "https://localhost/pay/l-9", currencyCode = "SAR" }));

        var link = await harness.Client.CheckoutLinks.CreateAsync(new CreateCheckoutLinkRequest { Amount = 1m, Channel = CheckoutLinkChannel.Qr });

        Assert.Equal("l-9", link.Id);
        Assert.Equal("https://localhost/pay/l-9", link.Url);
        Assert.Equal("SAR", link.Currency);
    }

    [Fact]
    public async Task Cancels_a_link()
    {
        using var harness = new Harness();
        harness.Handler.Enqueue(200, Fixtures.Envelope("SUCCESS", new { id = "l-1", status = "Cancelled" }));

        var link = await harness.Client.CheckoutLinks.CancelAsync("l-1");

        Assert.Equal(HttpMethod.Post, harness.LastRequest.Method);
        Assert.Equal("/api/v1/merchant/checkout-links/l-1/cancel", harness.LastRequest.Uri.AbsolutePath);
        Assert.Equal(OrderStatus.Cancelled, link.Status);
    }

    [Fact]
    public async Task Cancelling_a_closed_link_is_a_conflict()
    {
        using var harness = new Harness();
        harness.Handler.Enqueue(409, Fixtures.Envelope("NOT_OPEN", null, success: false, message: "Link is no longer open"));

        var ex = await Assert.ThrowsAsync<ConflictException>(() => harness.Client.CheckoutLinks.CancelAsync("l-1"));

        Assert.Equal("NOT_OPEN", ex.Code);
        Assert.Single(harness.Handler.Requests);
    }

    [Fact]
    public async Task A_duplicate_reference_on_a_retry_is_the_earlier_link()
    {
        using var harness = new Harness();
        harness.Handler.Enqueue(503).Enqueue(409, Fixtures.Duplicate);

        var link = await harness.Client.CheckoutLinks.CreateAsync(new CreateCheckoutLinkRequest { Amount = 1m, Channel = CheckoutLinkChannel.Qr, OrderReferenceId = "ORDER-1" });

        Assert.Equal("9a2e6c4b-1111-4c2d-8a1f-000000000001", link.Id);
        Assert.Equal("https://localhost/checkout/abc", link.Url);
    }
}
