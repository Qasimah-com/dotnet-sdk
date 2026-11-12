using Qasimha.Sdk.Tests.Support;
using Xunit;

namespace Qasimha.Sdk.Tests;

public class BasketValidationTests
{
    [Fact]
    public async Task A_basket_that_adds_up_is_sent()
    {
        using var harness = new Harness();
        harness.Handler.Enqueue(201, Fixtures.SessionCreated);

        await harness.Client.Checkout.CreateSessionAsync(Baskets.ThreeLine());

        Assert.Single(harness.Handler.Requests);
    }

    [Fact]
    public async Task Trailing_zeros_beyond_two_decimals_are_fine()
    {
        using var harness = new Harness();
        harness.Handler.Enqueue(201, Fixtures.SessionCreated);
        var request = Baskets.ThreeLine();
        request.TotalAmount = 1302.1000m;

        await harness.Client.Checkout.CreateSessionAsync(request);

        Assert.Single(harness.Handler.Requests);
    }

    [Fact]
    public async Task Names_the_line_whose_total_does_not_match()
    {
        using var harness = new Harness();
        var request = Baskets.ThreeLine();
        request.Items[1].TotalAmount = 130.00m;

        var ex = await Assert.ThrowsAsync<ValidationException>(() => harness.Client.Checkout.CreateSessionAsync(request));

        var error = Assert.Single(ex.Errors);
        Assert.Equal("Items[1].TotalAmount", error.Field);
        Assert.Equal("expected 135.00 (3 x 45.00 - 0.00), got 130.00", error.Message);
        Assert.StartsWith("Items[1].TotalAmount: expected 135.00", ex.Message, StringComparison.Ordinal);
        Assert.Empty(harness.Handler.Requests);
    }

    [Fact]
    public async Task Names_the_order_total_when_the_lines_do_not_sum_to_it()
    {
        using var harness = new Harness();
        var request = Baskets.Single("CART-C", 10.00m, Baskets.Item("SKU-9", "Laptop", ItemType.Physical, 1, 5000.00m));

        var ex = await Assert.ThrowsAsync<ValidationException>(() => harness.Client.Checkout.CreateSessionAsync(request));

        var error = Assert.Single(ex.Errors);
        Assert.Equal("TotalAmount", error.Field);
        Assert.Equal("expected 5000.00 (items 5000.00 + tax 0.00 + shipping 0.00 - discount 0.00), got 10.00", error.Message);
        Assert.Empty(harness.Handler.Requests);
    }

    [Fact]
    public async Task Applies_item_discounts_to_the_line_identity()
    {
        using var harness = new Harness();
        harness.Handler.Enqueue(201, Fixtures.SessionCreated);
        var item = Baskets.Item("SKU-D", "Discounted", ItemType.Physical, 2, 50.00m);
        item.DiscountAmount = 10.00m;
        item.TotalAmount = 90.00m;

        await harness.Client.Checkout.CreateSessionAsync(Baskets.Single("CART-D", 90.00m, item));

        Assert.Single(harness.Handler.Requests);
    }

    [Fact]
    public async Task Refuses_more_than_two_decimals_before_sending()
    {
        using var harness = new Harness();
        var request = Baskets.ThreeLine();
        request.TaxAmount = 173.105m;

        var ex = await Assert.ThrowsAsync<ValidationException>(() => harness.Client.Checkout.CreateSessionAsync(request));

        var error = Assert.Single(ex.Errors);
        Assert.Equal("TaxAmount", error.Field);
        Assert.Contains("two decimals", error.Message, StringComparison.Ordinal);
        Assert.Empty(harness.Handler.Requests);
    }

    [Fact]
    public async Task Refuses_a_negative_unit_price()
    {
        using var harness = new Harness();
        var item = Baskets.Item("SKU-N", "Negative", ItemType.Physical, 1, -100.00m);

        var ex = await Assert.ThrowsAsync<ValidationException>(() => harness.Client.Checkout.CreateSessionAsync(Baskets.Single("CART-E2", 100.00m, item)));

        Assert.Contains(ex.Errors, e => e.Field == "Items[0].UnitPrice");
        Assert.Empty(harness.Handler.Requests);
    }

    [Fact]
    public async Task Refuses_a_zero_quantity()
    {
        using var harness = new Harness();
        var item = Baskets.Item("SKU-0", "Ghost", ItemType.Physical, 0, 100.00m);

        var ex = await Assert.ThrowsAsync<ValidationException>(() => harness.Client.Checkout.CreateSessionAsync(Baskets.Single("CART-E", 100.00m, item)));

        Assert.Contains(ex.Errors, e => e.Field == "Items[0].Quantity");
    }

    [Fact]
    public async Task Refuses_an_empty_cart()
    {
        using var harness = new Harness();
        var request = Baskets.ThreeLine();
        request.Items = Array.Empty<CheckoutItem>();

        var ex = await Assert.ThrowsAsync<ValidationException>(() => harness.Client.Checkout.CreateSessionAsync(request));

        var error = Assert.Single(ex.Errors);
        Assert.Equal("Items", error.Field);
    }

    [Fact]
    public async Task Requires_a_shipping_address()
    {
        using var harness = new Harness();
        var request = Baskets.ThreeLine();
        request.ShippingAddress = null!;

        var ex = await Assert.ThrowsAsync<ValidationException>(() => harness.Client.Checkout.CreateSessionAsync(request));

        var error = Assert.Single(ex.Errors);
        Assert.Equal("ShippingAddress", error.Field);
    }

    [Fact]
    public async Task Requires_the_address_lines_the_api_needs()
    {
        using var harness = new Harness();
        var request = Baskets.ThreeLine();
        request.ShippingAddress.Line1 = "";
        request.BillingAddress = new Address { FirstName = "N", LastName = "A", Line1 = "L", City = "", CountryCode = "SA" };

        var ex = await Assert.ThrowsAsync<ValidationException>(() => harness.Client.Checkout.CreateSessionAsync(request));

        Assert.Contains(ex.Errors, e => e.Field == "ShippingAddress.Line1");
        Assert.Contains(ex.Errors, e => e.Field == "BillingAddress.City");
    }

    [Theory]
    [InlineData("pay_later")]
    [InlineData("")]
    [InlineData("PAY_NOW")]
    public async Task Refuses_an_unknown_payment_type(string paymentType)
    {
        using var harness = new Harness();
        var request = Baskets.ThreeLine();
        request.PaymentType = paymentType;

        var ex = await Assert.ThrowsAsync<ValidationException>(() => harness.Client.Checkout.CreateSessionAsync(request));

        Assert.Contains(ex.Errors, e => e.Field == "PaymentType");
    }

    [Theory]
    [InlineData(CheckoutPaymentType.PayByInstalments)]
    [InlineData(CheckoutPaymentType.PayNow)]
    [InlineData(CheckoutPaymentType.PayNextMonth)]
    [InlineData(CheckoutPaymentType.PayInFull)]
    public async Task Accepts_every_known_payment_type(string paymentType)
    {
        using var harness = new Harness();
        harness.Handler.Enqueue(201, Fixtures.SessionCreated);
        var request = Baskets.ThreeLine();
        request.PaymentType = paymentType;

        await harness.Client.Checkout.CreateSessionAsync(request);

        Assert.Contains("\"paymentType\":\"" + paymentType + "\"", harness.LastRequest.Body, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("3123456789")]
    [InlineData("12345")]
    [InlineData("12345678901")]
    [InlineData("1abc567890")]
    public async Task Refuses_a_malformed_national_id(string nationalId)
    {
        using var harness = new Harness();
        var request = Baskets.ThreeLine();
        request.Consumer.NationalId = nationalId;

        var ex = await Assert.ThrowsAsync<ValidationException>(() => harness.Client.Checkout.CreateSessionAsync(request));

        Assert.Contains(ex.Errors, e => e.Field == "Consumer.NationalId");
    }

    [Theory]
    [InlineData("1234567890")]
    [InlineData("2234567890")]
    [InlineData(null)]
    public async Task Accepts_a_well_formed_or_absent_national_id(string? nationalId)
    {
        using var harness = new Harness();
        harness.Handler.Enqueue(201, Fixtures.SessionCreated);
        var request = Baskets.ThreeLine();
        request.Consumer.NationalId = nationalId;

        await harness.Client.Checkout.CreateSessionAsync(request);

        Assert.Single(harness.Handler.Requests);
    }

    [Fact]
    public async Task Refuses_a_reference_longer_than_255_characters()
    {
        using var harness = new Harness();
        var request = Baskets.ThreeLine(new string('x', 256));

        var ex = await Assert.ThrowsAsync<ValidationException>(() => harness.Client.Checkout.CreateSessionAsync(request));

        Assert.Contains(ex.Errors, e => e.Field == "OrderReferenceId");
    }

    [Fact]
    public async Task Requires_consumer_names_and_phone()
    {
        using var harness = new Harness();
        var request = Baskets.ThreeLine();
        request.Consumer.FirstName = " ";
        request.Consumer.PhoneNumber = "";

        var ex = await Assert.ThrowsAsync<ValidationException>(() => harness.Client.Checkout.CreateSessionAsync(request));

        Assert.Contains(ex.Errors, e => e.Field == "Consumer.FirstName");
        Assert.Contains(ex.Errors, e => e.Field == "Consumer.PhoneNumber");
    }

    [Fact]
    public async Task Collects_every_error_and_counts_the_rest_in_the_message()
    {
        using var harness = new Harness();
        var request = Baskets.ThreeLine("");
        request.Items[0].Name = "";
        request.Items[2].Quantity = 0;

        var ex = await Assert.ThrowsAsync<ValidationException>(() => harness.Client.Checkout.CreateSessionAsync(request));

        Assert.Equal(3, ex.Errors.Count);
        Assert.Equal("OrderReferenceId: is required (+2 more)", ex.Message);
    }

    [Fact]
    public async Task A_refund_amount_must_have_two_decimals()
    {
        using var harness = new Harness();

        var ex = await Assert.ThrowsAsync<ValidationException>(() => harness.Client.Orders.RefundAsync("o-1", new RefundRequest { Amount = 10.005m }));

        Assert.Contains(ex.Errors, e => e.Field == "Amount");
        Assert.Empty(harness.Handler.Requests);
    }

    [Fact]
    public async Task A_shipped_amount_must_not_be_negative()
    {
        using var harness = new Harness();

        var ex = await Assert.ThrowsAsync<ValidationException>(() => harness.Client.Orders.FulfillAsync("o-1", new FulfillRequest { ShippedAmount = -1m }));

        Assert.Contains(ex.Errors, e => e.Field == "ShippedAmount");
        Assert.Empty(harness.Handler.Requests);
    }

    [Fact]
    public async Task A_qr_session_applies_the_identities()
    {
        using var harness = new Harness();
        var request = Baskets.Qr();
        request.TotalAmount = 1.00m;

        var ex = await Assert.ThrowsAsync<ValidationException>(() => harness.Client.Checkout.CreateQrSessionAsync(request));

        Assert.Contains(ex.Errors, e => e.Field == "TotalAmount");
        Assert.Empty(harness.Handler.Requests);
    }
}
