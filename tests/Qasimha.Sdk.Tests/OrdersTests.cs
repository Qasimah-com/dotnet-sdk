using System.Text;
using System.Text.Json;
using Qasimha.Sdk.Tests.Support;
using Xunit;

namespace Qasimha.Sdk.Tests;

public class OrdersTests
{
    private static readonly string[] InternalFields =
    {
        "DecisionGateTrace", "DecisionFeatures", "DecisionReasons", "DecisionId", "DecisionStatus", "DeclinedCode",
        "DeclinedReason", "Score", "RiskBand", "MdrRate", "Apr", "AdminFee", "AdminFeeVat", "ProcessingFee", "ProcessingFeeVat",
        "PlanOffersJson", "PlanQuotesJson", "ContractAcceptedIp", "ContractAcceptedDevice", "DomainEvents", "IsDeleted",
        "CreatedBy", "UpdatedBy", "LynkStatus", "LynkRequired", "SanadId", "PromissoryNoteStatus", "DpdDays", "DpdBucket",
        "WriteOffReason", "Merchant", "MerchantId",
    };

    [Fact]
    public async Task Lists_orders_from_the_recorded_fixture()
    {
        using var harness = new Harness();
        harness.Handler.Enqueue(200, Fixtures.Read("orders-list.json"));

        var page = await harness.Client.Orders.ListAsync(new ListOrdersRequest { PageSize = 2 });

        Assert.Equal("/api/v1/merchant/orders", harness.LastRequest.Uri.AbsolutePath);
        Assert.Equal(2, page.Items.Count);
        Assert.Equal(19, page.Pagination.TotalItems);
        Assert.Equal(1, page.Pagination.Page);
        Assert.Equal(2, page.Pagination.PageSize);
        Assert.Equal(10, page.Pagination.TotalPages);

        var row = page.Items[0];
        Assert.Equal("5c4a3eb5-c2b1-4d0f-b32a-062c379cfeac", row.Id);
        Assert.Equal("CART-101412-9299-G", row.OrderReferenceId);
        Assert.Equal(OrderStatus.Declined, row.Status);
        Assert.Equal(OrderChannel.Online, row.Channel);
        Assert.Equal("BNPL_POLICY_V41", row.PolicyCode);
        Assert.Equal("BNPL", row.PolicyType);
        Assert.Equal(250.00m, row.TotalAmount);
        Assert.Equal("SAR", row.CurrencyCode);
        Assert.Equal(new DateTimeOffset(2026, 9, 25, 10, 14, 13, TimeSpan.Zero).AddTicks(5922180), row.CreatedAt);
        Assert.Equal("d28f47d0-4435-4604-9b76-70d129c20372", row.CustomerId);
        Assert.Equal("Noura Al-Shammari", row.CustomerName);
        Assert.Equal("cart-shopper@qasimha.test", row.CustomerEmail);
        Assert.Equal("+966512349999", row.CustomerPhone);
        Assert.Equal(4, row.Instalments);
        Assert.Equal(0m, row.CapturedAmount);
        Assert.Equal(0m, row.RefundedAmount);
        Assert.Null(row.EjarContractNumber);

        Assert.Equal("c1e4f312-f61c-445f-bce4-8551984db9f3", page.Items[1].Id);
        Assert.Equal(1302.10m, page.Items[1].TotalAmount);
    }

    [Fact]
    public async Task Reads_an_order_from_the_recorded_fixture()
    {
        using var harness = new Harness();
        harness.Handler.Enqueue(200, Fixtures.Read("order-detail.declined.json"));

        var order = await harness.Client.Orders.GetAsync("5c4a3eb5-c2b1-4d0f-b32a-062c379cfeac");

        Assert.Equal("/api/v1/merchant/orders/5c4a3eb5-c2b1-4d0f-b32a-062c379cfeac", harness.LastRequest.Uri.AbsolutePath);
        AssertRecordedDeclinedOrder(order);
        Assert.Equal("44d0220d-09a5-4c0e-9e08-288ca3f71f3c", order.RequestId);
    }

    [Fact]
    public async Task Reads_an_order_by_reference_from_the_recorded_fixture()
    {
        using var harness = new Harness();
        harness.Handler.Enqueue(200, Fixtures.Read("order-by-reference.json"));

        var order = await harness.Client.Orders.GetByReferenceAsync("CART-101412-9299-G");

        Assert.Equal(HttpMethod.Get, harness.LastRequest.Method);
        Assert.Equal("/api/v1/merchant/orders/by-reference/CART-101412-9299-G", harness.LastRequest.Uri.AbsolutePath);
        Assert.Null(harness.LastRequest.Header("Idempotency-Key"));
        AssertRecordedDeclinedOrder(order);
        Assert.Equal("22e61620-9158-4afd-ac79-a362a10ac7d5", order.RequestId);
    }

    [Theory]
    [InlineData("ORDER/2026/1", "ORDER/2026/1")]
    [InlineData("ORDER 1&2#3?4%", "ORDER%201%262%233%3F4%25")]
    [InlineData("  ORDER-7  ", "ORDER-7")]
    [InlineData("A//B/.x/x.", "A//B/.x/x.")]
    [InlineData("طلب/٢٠٢٦", "%D8%B7%D9%84%D8%A8/%D9%A2%D9%A0%D9%A2%D9%A6")]
    public async Task Encodes_each_reference_segment_and_keeps_the_slashes(string reference, string expectedPath)
    {
        using var harness = new Harness();
        harness.Handler.Enqueue(200, Fixtures.Read("order-by-reference.json"));

        await harness.Client.Orders.GetByReferenceAsync(reference);

        Assert.Equal("/api/v1/merchant/orders/by-reference/" + expectedPath, harness.LastRequest.Uri.AbsolutePath);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData(".")]
    [InlineData("A/../B")]
    [InlineData("A/./B")]
    public async Task Refuses_an_empty_or_dot_segment_reference(string reference)
    {
        using var harness = new Harness();

        await Assert.ThrowsAsync<ArgumentException>(() => harness.Client.Orders.GetByReferenceAsync(reference));
        Assert.Empty(harness.Handler.Requests);
    }

    [Fact]
    public async Task An_unknown_reference_is_not_found()
    {
        using var harness = new Harness();
        harness.Handler.Enqueue(404, Fixtures.Envelope("NOT_FOUND", null, success: false, message: "Order not found"));

        var ex = await Assert.ThrowsAsync<NotFoundException>(() => harness.Client.Orders.GetByReferenceAsync("ORDER-NEVER"));

        Assert.Equal("NOT_FOUND", ex.Code);
        Assert.Equal(404, ex.HttpStatus);
    }

    [Fact]
    public async Task A_key_without_orders_read_cannot_look_up_by_reference()
    {
        using var harness = new Harness();
        harness.Handler.Enqueue(403);

        await Assert.ThrowsAsync<PermissionException>(() => harness.Client.Orders.GetByReferenceAsync("ORDER-1"));
    }

    private static void AssertRecordedDeclinedOrder(Order order)
    {
        Assert.Equal("5c4a3eb5-c2b1-4d0f-b32a-062c379cfeac", order.Id);
        Assert.Equal("CART-101412-9299-G", order.OrderReferenceId);
        Assert.Equal(OrderStatus.Declined, order.Status);
        Assert.Equal(OrderChannel.Online, order.Channel);
        Assert.Equal(PaymentType.PayByInstalments, order.PaymentType);
        Assert.Null(order.CheckoutChannel);
        Assert.Equal(4, order.Instalments);
        Assert.Equal("SA", order.CountryCode);
        Assert.Equal("SAR", order.CurrencyCode);
        Assert.Equal(250.00m, order.TotalAmount);
        Assert.Equal(0m, order.TaxAmount);
        Assert.Equal(0m, order.ShippingAmount);
        Assert.Equal(0m, order.DiscountAmount);
        Assert.Equal("Cart contract test", order.Description);
        Assert.Null(order.ApprovedAmount);
        Assert.Equal(0m, order.DownPaymentAmount);
        Assert.Equal(0m, order.CapturedAmount);
        Assert.Equal(0m, order.RefundableAmount);
        Assert.Equal(0m, order.CollectedAmount);
        Assert.Null(order.PaymentUrl);
        Assert.Null(order.ConfirmedAt);
        Assert.Equal(new DateTimeOffset(2026, 9, 25, 10, 14, 13, TimeSpan.Zero).AddTicks(5922180), order.CreatedAt);
        Assert.Null(order.ContractNumber);
        Assert.Null(order.NotificationLanguage);

        Assert.NotNull(order.Customer);
        Assert.Equal("d28f47d0-4435-4604-9b76-70d129c20372", order.Customer!.Id);
        Assert.Equal("Noura Al-Shammari", order.Customer.Name);
        Assert.Equal("cart-shopper@qasimha.test", order.Customer.Email);
        Assert.Equal("+966512349999", order.Customer.Phone);
        Assert.Null(order.Customer.NationalId);

        var item = Assert.Single(order.Items);
        Assert.Equal("SKU-AR", item.ReferenceId);
        Assert.Equal("SKU-AR", item.Sku);
        Assert.Equal("سماعات لاسلكية", item.Name);
        Assert.Equal(ItemType.Physical, item.Type);
        Assert.Equal(1, item.Quantity);
        Assert.Equal(250.00m, item.UnitPrice);
        Assert.Equal(250.00m, item.TotalAmount);
        Assert.Null(item.ImageUrl);

        Assert.Empty(order.Installments);
        Assert.Empty(order.Fulfillments);
        Assert.Empty(order.Reversals);
        Assert.Empty(order.Voids);

        Assert.Equal(3, order.StatusHistory.Count);
        var declined = Assert.Single(order.StatusHistory, h => h.ToStatus == OrderStatus.Declined);
        Assert.Equal(OrderStatus.Draft, declined.FromStatus);
        Assert.Equal("DecisionDeclined", declined.Trigger);
        Assert.All(order.StatusHistory, h => Assert.Null(h.Metadata));
    }

    [Fact]
    public void Internal_platform_fields_are_not_typed_members()
    {
        var members = typeof(Order).GetProperties().Select(p => p.Name).ToHashSet(StringComparer.Ordinal);

        foreach (var field in InternalFields)
        {
            Assert.DoesNotContain(field, members);
        }
    }

    [Fact]
    public async Task Fields_without_a_typed_member_stay_reachable_through_raw()
    {
        using var harness = new Harness();
        harness.Handler.Enqueue(200, Fixtures.Envelope("SUCCESS", new { id = "o-1", status = "Declined", policyCode = "BNPL_POLICY_V41", settlementModel = 1 }));

        var order = await harness.Client.Orders.GetAsync("o-1");

        Assert.DoesNotContain("PolicyCode", typeof(Order).GetProperties().Select(p => p.Name));
        Assert.Equal("BNPL_POLICY_V41", order.Raw.GetProperty("policyCode").GetString());
        Assert.Equal(1, order.Raw.GetProperty("settlementModel").GetInt32());
    }

    [Fact]
    public async Task The_recorded_detail_carries_no_decision_internals()
    {
        using var harness = new Harness();
        harness.Handler.Enqueue(200, Fixtures.Read("order-detail.declined.json"));

        var order = await harness.Client.Orders.GetAsync("5c4a3eb5-c2b1-4d0f-b32a-062c379cfeac");

        foreach (var property in order.Raw.EnumerateObject())
        {
            Assert.DoesNotContain(property.Name, InternalFields, StringComparer.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public async Task Captures_an_order()
    {
        using var harness = new Harness();
        harness.Handler.Enqueue(200, Fixtures.Envelope("SUCCESS", new { id = "o-1", status = "Confirmed", confirmedAt = "2026-06-11T09:30:00Z" }));

        var result = await harness.Client.Orders.CaptureAsync("o-1");

        Assert.Equal(HttpMethod.Post, harness.LastRequest.Method);
        Assert.Equal("/api/v1/merchant/orders/o-1/capture", harness.LastRequest.Uri.AbsolutePath);
        Assert.Null(harness.LastRequest.Body);
        Assert.NotNull(harness.LastRequest.Header("Idempotency-Key"));
        Assert.Equal("o-1", result.Id);
        Assert.Equal(OrderStatus.Confirmed, result.Status);
        Assert.Equal(new DateTimeOffset(2026, 6, 11, 9, 30, 0, TimeSpan.Zero), result.ConfirmedAt);
    }

    [Fact]
    public async Task Cancels_an_order()
    {
        using var harness = new Harness();
        harness.Handler.Enqueue(200, Fixtures.Envelope("SUCCESS", new { id = "o-1", status = 10 }));

        var result = await harness.Client.Orders.CancelAsync("o-1");

        Assert.Equal("/api/v1/merchant/orders/o-1/cancel", harness.LastRequest.Uri.AbsolutePath);
        Assert.Equal(OrderStatus.Voided, result.Status);
    }

    [Fact]
    public async Task Refunds_an_order()
    {
        using var harness = new Harness();
        harness.Handler.Enqueue(200, Fixtures.Envelope("SUCCESS", new { id = "o-1", status = "PartiallyReversed", refundedAmount = 300.00 }));

        var result = await harness.Client.Orders.RefundAsync("o-1", new RefundRequest { Amount = 300.00m, Reason = "Customer return" });

        Assert.Equal("/api/v1/merchant/orders/o-1/refund", harness.LastRequest.Uri.AbsolutePath);
        Assert.Equal("{\"amount\":300.00,\"reason\":\"Customer return\"}", harness.LastRequest.Body);
        Assert.Equal(300.00m, result.RefundedAmount);
        Assert.Equal(OrderStatus.PartiallyReversed, result.Status);
    }

    [Fact]
    public async Task Fulfils_everything_outstanding_with_an_empty_body()
    {
        using var harness = new Harness();
        harness.Handler.Enqueue(200, Fixtures.Envelope("SUCCESS", new { id = "o-1", status = 6, fulfilledAmount = 1302.10 }));

        var result = await harness.Client.Orders.FulfillAsync("o-1");

        Assert.Equal("/api/v1/merchant/orders/o-1/fulfill", harness.LastRequest.Uri.AbsolutePath);
        Assert.Equal("{}", harness.LastRequest.Body);
        Assert.Equal(OrderStatus.FullyFulfilled, result.Status);
        Assert.Equal(1302.10m, result.FulfilledAmount);
    }

    [Fact]
    public async Task Fulfils_a_part_with_shipment_details()
    {
        using var harness = new Harness();
        harness.Handler.Enqueue(200, Fixtures.Envelope("SUCCESS", new { id = "o-1", status = "PartiallyFulfilled" }));

        var result = await harness.Client.Orders.FulfillAsync("o-1", new FulfillRequest
        {
            ShippedAmount = 1200.00m,
            ShippingCompany = "Aramex",
            TrackingNumber = "ARX123",
        });

        Assert.Equal("{\"shippedAmount\":1200.00,\"shippingCompany\":\"Aramex\",\"trackingNumber\":\"ARX123\"}", harness.LastRequest.Body);
        Assert.Null(result.FulfilledAmount);
    }

    [Fact]
    public async Task Reads_the_counts()
    {
        using var harness = new Harness();
        harness.Handler.Enqueue(200, Fixtures.Envelope("SUCCESS", new { all = 7, approved = 1, authorised = 2, captured = 3, refunded = 0, cancelled = 1 }));

        var counts = await harness.Client.Orders.CountsAsync();

        Assert.Equal("/api/v1/merchant/orders/counts", harness.LastRequest.Uri.AbsolutePath);
        Assert.Equal(7, counts.All);
        Assert.Equal(1, counts.Approved);
        Assert.Equal(2, counts.Authorised);
        Assert.Equal(3, counts.Captured);
        Assert.Equal(0, counts.Refunded);
        Assert.Equal(1, counts.Cancelled);
    }

    [Fact]
    public async Task Downloads_the_contract_as_bytes()
    {
        using var harness = new Harness();
        var pdf = Encoding.ASCII.GetBytes("%PDF-1.7 test");
        harness.Handler.EnqueueBytes(200, pdf, "application/pdf", "contract.pdf");

        var contract = await harness.Client.Orders.ContractAsync("o-1");

        Assert.Equal("/api/v1/merchant/orders/o-1/contract", harness.LastRequest.Uri.AbsolutePath);
        Assert.StartsWith("application/pdf", harness.LastRequest.Header("Accept"), StringComparison.Ordinal);
        Assert.Equal(pdf, contract.Content);
        Assert.Equal("application/pdf", contract.ContentType);
        Assert.Equal("contract.pdf", contract.FileName);
        Assert.Equal(200, contract.Response!.HttpStatus);
    }

    [Fact]
    public async Task A_missing_contract_is_not_found()
    {
        using var harness = new Harness();
        harness.Handler.Enqueue(404, Fixtures.Envelope("NOT_FOUND", null, success: false));

        await Assert.ThrowsAsync<NotFoundException>(() => harness.Client.Orders.ContractAsync("o-1"));
    }

    [Fact]
    public async Task Escapes_the_order_id_in_the_path()
    {
        using var harness = new Harness();
        harness.Handler.Enqueue(404, Fixtures.Envelope("NOT_FOUND", null, success: false));

        await Assert.ThrowsAsync<NotFoundException>(() => harness.Client.Orders.GetAsync("a b/../c"));

        Assert.Equal("/api/v1/merchant/orders/a%20b%2F..%2Fc", harness.LastRequest.Uri.AbsolutePath);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public async Task Refuses_an_empty_order_id(string id)
    {
        using var harness = new Harness();

        await Assert.ThrowsAsync<ArgumentException>(() => harness.Client.Orders.GetAsync(id));
        await Assert.ThrowsAsync<ArgumentException>(() => harness.Client.Orders.CaptureAsync(id));
        await Assert.ThrowsAsync<ArgumentNullException>(() => harness.Client.Orders.RefundAsync("o-1", null!));
        Assert.Empty(harness.Handler.Requests);
    }

    [Fact]
    public async Task Handles_rows_under_items_and_a_missing_pagination()
    {
        using var harness = new Harness();
        harness.Handler.Enqueue(200, Fixtures.Envelope("SUCCESS", new { items = new[] { new { id = "o-1", status = 5 } } }));

        var page = await harness.Client.Orders.ListAsync(new ListOrdersRequest { Page = 3, PageSize = 10 });

        var row = Assert.Single(page.Items);
        Assert.Equal(OrderStatus.Confirmed, row.Status);
        Assert.Equal(3, page.Pagination.Page);
        Assert.Equal(10, page.Pagination.PageSize);
        Assert.Equal(1, page.Pagination.TotalItems);
    }

    [Fact]
    public async Task Tolerates_null_collections_and_null_amounts()
    {
        using var harness = new Harness();
        harness.Handler.Enqueue(200, Fixtures.Envelope("SUCCESS", new { id = "o-9", status = "New", items = (object?)null, totalAmount = (object?)null, approvedAmount = (object?)null }));

        var order = await harness.Client.Orders.GetAsync("o-9");

        Assert.Empty(order.Items);
        Assert.Empty(order.StatusHistory);
        Assert.Equal(0m, order.TotalAmount);
        Assert.Null(order.ApprovedAmount);
        Assert.Equal(JsonValueKind.Null, order.Raw.GetProperty("items").ValueKind);
    }
}
