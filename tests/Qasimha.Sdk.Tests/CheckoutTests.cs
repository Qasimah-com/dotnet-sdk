using System.Text.Json;
using Qasimha.Sdk.Tests.Support;
using Xunit;

namespace Qasimha.Sdk.Tests;

public class CheckoutTests
{
    [Fact]
    public async Task Creates_a_session()
    {
        using var harness = new Harness();
        harness.Handler.Enqueue(201, Fixtures.SessionCreated);

        var session = await harness.Client.Checkout.CreateSessionAsync(Baskets.ThreeLine());

        Assert.Equal("9a2e6c4b-1111-4c2d-8a1f-000000000001", session.OrderId);
        Assert.Equal("https://localhost/checkout/abc", session.PaymentUrl);
        Assert.Equal("sess_1", session.ProviderSessionId);
        Assert.Equal(OrderStatus.New, session.Status);
        Assert.Equal("11111111-1111-4111-8111-111111111111", session.RequestId);
        Assert.Equal(201, session.Response!.HttpStatus);
    }

    [Fact]
    public async Task Serialises_the_basket_as_the_api_expects()
    {
        using var harness = new Harness();
        harness.Handler.Enqueue(201, Fixtures.SessionCreated);
        var request = Baskets.ThreeLine("CART-1");
        request.MerchantUrl = new MerchantUrl { Success = "https://localhost/ok", Failure = "https://localhost/ko", Cancel = "https://localhost/cancel" };
        request.Consumer.NationalId = "1234567890";

        await harness.Client.Checkout.CreateSessionAsync(request);

        var body = harness.LastRequest.Body!;
        Assert.Contains("\"totalAmount\":1302.10", body, StringComparison.Ordinal);
        Assert.Contains("\"taxAmount\":173.10", body, StringComparison.Ordinal);
        Assert.Contains("\"unitPrice\":45.00", body, StringComparison.Ordinal);
        Assert.Contains("\"quantity\":3", body, StringComparison.Ordinal);
        Assert.Contains("\"paymentType\":\"pay_by_instalments\"", body, StringComparison.Ordinal);
        Assert.Contains("\"instalments\":4", body, StringComparison.Ordinal);
        Assert.Contains("\"currencyCode\":\"SAR\"", body, StringComparison.Ordinal);
        Assert.Contains("\"countryCode\":\"SA\"", body, StringComparison.Ordinal);
        Assert.Contains("\"nationalId\":\"1234567890\"", body, StringComparison.Ordinal);
        Assert.DoesNotContain("billingAddress", body, StringComparison.Ordinal);
        Assert.DoesNotContain("riskAssessment", body, StringComparison.Ordinal);
        Assert.DoesNotContain("merchantId", body, StringComparison.Ordinal);
        Assert.DoesNotContain("decisionFeatures", body, StringComparison.Ordinal);

        using var document = JsonDocument.Parse(body);
        var root = document.RootElement;
        Assert.Equal("CART-1", root.GetProperty("orderReferenceId").GetString());
        Assert.Equal(JsonValueKind.Number, root.GetProperty("totalAmount").ValueKind);
        Assert.Equal(3, root.GetProperty("items").GetArrayLength());
        Assert.Equal("SKU-3", root.GetProperty("items")[2].GetProperty("sku").GetString());
        Assert.Equal("digital", root.GetProperty("items")[2].GetProperty("type").GetString());
        Assert.Equal("https://localhost/ok", root.GetProperty("merchantUrl").GetProperty("success").GetString());
        Assert.Equal("King Fahd Rd", root.GetProperty("shippingAddress").GetProperty("line1").GetString());
        Assert.False(root.GetProperty("merchantUrl").TryGetProperty("notification", out _));
    }

    [Fact]
    public async Task An_arabic_product_name_round_trips_through_the_body()
    {
        using var harness = new Harness();
        harness.Handler.Enqueue(201, Fixtures.SessionCreated);
        var item = Baskets.Item("SKU-AR", "سماعات لاسلكية", ItemType.Physical, 1, 250.00m);

        await harness.Client.Checkout.CreateSessionAsync(Baskets.Single("CART-G", 250.00m, item));

        using var document = JsonDocument.Parse(harness.LastRequest.Body!);
        Assert.Equal("سماعات لاسلكية", document.RootElement.GetProperty("items")[0].GetProperty("name").GetString());
    }

    [Fact]
    public async Task A_duplicate_on_a_retry_is_the_earlier_create()
    {
        using var harness = new Harness();
        harness.Handler.Enqueue(503).Enqueue(409, Fixtures.Duplicate);

        var session = await harness.Client.Checkout.CreateSessionAsync(Baskets.ThreeLine());

        Assert.Equal("9a2e6c4b-1111-4c2d-8a1f-000000000001", session.OrderId);
        Assert.Equal("https://localhost/checkout/abc", session.PaymentUrl);
        Assert.Equal(OrderStatus.AwaitingCheckout, session.Status);
        Assert.Equal(2, session.Response!.Attempt);
        Assert.Equal("22222222-2222-4222-8222-222222222222", session.RequestId);
        var info = Assert.Single(harness.Logs, l => l.Level == QasimhaLogLevel.Info);
        Assert.Equal("DUPLICATE_ORDER_REFERENCE", info.Fields["code"]);
        Assert.Equal(2, info.Fields["attempt"]);
    }

    [Fact]
    public async Task A_duplicate_on_a_retry_without_an_order_id_is_still_an_error()
    {
        using var harness = new Harness();
        harness.Handler.Enqueue(503).Enqueue(409, Fixtures.DuplicateWithoutOrder);

        await Assert.ThrowsAsync<DuplicateOrderReferenceException>(() => harness.Client.Checkout.CreateSessionAsync(Baskets.ThreeLine()));
    }

    [Fact]
    public async Task A_declined_session_is_an_exception_with_the_order_id()
    {
        using var harness = new Harness();
        harness.Handler.Enqueue(422, Fixtures.Declined);

        var ex = await Assert.ThrowsAsync<OrderDeclinedException>(() => harness.Client.Checkout.CreateSessionAsync(Baskets.ThreeLine()));

        Assert.Equal("76c94cf2-db05-4c5a-b8da-bb05d1ae7cbc", ex.OrderId);
        Assert.Equal("The order was declined", ex.Message);
    }

    [Fact]
    public async Task Creates_a_pos_session()
    {
        using var harness = new Harness();
        harness.Handler.Enqueue(201, Fixtures.Envelope("SESSION_CREATED", new
        {
            installmentId = "pos-1",
            providerSessionId = (string?)null,
            checkoutUrl = "https://localhost/pay/pos-1",
            status = "AwaitingCheckout",
        }));

        var session = await harness.Client.Checkout.CreatePosSessionAsync(Baskets.Pos());

        Assert.Equal("pos-1", session.OrderId);
        Assert.Equal("https://localhost/pay/pos-1", session.CheckoutUrl);
        Assert.Equal(OrderStatus.AwaitingCheckout, session.Status);
        Assert.EndsWith("/api/v1/pay/pos/create", harness.LastRequest.Uri.AbsolutePath, StringComparison.Ordinal);
        Assert.Contains("\"email\":\"shopper@example.com\"", harness.LastRequest.Body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_pos_session_needs_an_email()
    {
        using var harness = new Harness();
        var request = Baskets.Pos();
        request.Consumer.Email = null;

        var ex = await Assert.ThrowsAsync<ValidationException>(() => harness.Client.Checkout.CreatePosSessionAsync(request));

        Assert.Contains(ex.Errors, e => e.Field == "Consumer.Email");
        Assert.Empty(harness.Handler.Requests);
    }

    [Fact]
    public async Task A_pos_session_applies_the_basket_identities()
    {
        using var harness = new Harness();
        var request = Baskets.Pos();
        request.TotalAmount = 10.00m;

        var ex = await Assert.ThrowsAsync<ValidationException>(() => harness.Client.Checkout.CreatePosSessionAsync(request));

        Assert.Contains(ex.Errors, e => e.Field == "TotalAmount");
        Assert.Empty(harness.Handler.Requests);
    }

    [Fact]
    public async Task Creates_a_qr_session()
    {
        using var harness = new Harness();
        harness.Handler.Enqueue(201, Fixtures.Envelope("SESSION_CREATED", new { installmentId = "qr-1", qrCodeUrl = "https://localhost/pay/qr-1", status = 12 }));

        var session = await harness.Client.Checkout.CreateQrSessionAsync(Baskets.Qr());

        Assert.Equal("qr-1", session.OrderId);
        Assert.Equal("https://localhost/pay/qr-1", session.QrCodeUrl);
        Assert.Equal(OrderStatus.AwaitingCheckout, session.Status);
        Assert.EndsWith("/api/v1/pay/qr/create", harness.LastRequest.Uri.AbsolutePath, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_declined_qr_session_answers_like_the_other_endpoints()
    {
        using var harness = new Harness();
        harness.Handler.Enqueue(201, Fixtures.Envelope("SESSION_CREATED", new { installmentId = "qr-2", qrCodeUrl = "", status = "Declined" }));

        var ex = await Assert.ThrowsAsync<OrderDeclinedException>(() => harness.Client.Checkout.CreateQrSessionAsync(Baskets.Qr()));

        Assert.Equal("qr-2", ex.OrderId);
        Assert.Equal("ORDER_DECLINED", ex.Code);
        Assert.Equal(201, ex.HttpStatus);
        Assert.Equal("4d51526b-617e-4c37-a3f3-20f209d87b69", ex.RequestId);
    }

    [Fact]
    public async Task Rejects_a_null_request()
    {
        using var harness = new Harness();

        await Assert.ThrowsAsync<ArgumentNullException>(() => harness.Client.Checkout.CreateSessionAsync(null!));
        await Assert.ThrowsAsync<ArgumentNullException>(() => harness.Client.Checkout.CreatePosSessionAsync(null!));
        await Assert.ThrowsAsync<ArgumentNullException>(() => harness.Client.Checkout.CreateQrSessionAsync(null!));
    }
}
