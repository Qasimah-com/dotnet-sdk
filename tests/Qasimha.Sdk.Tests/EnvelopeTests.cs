using System.Text.Json;
using Qasimha.Sdk.Tests.Support;
using Xunit;

namespace Qasimha.Sdk.Tests;

public class EnvelopeTests
{
    [Fact]
    public async Task Exposes_the_envelope_uuid_as_the_request_id()
    {
        using var harness = new Harness();
        harness.Handler.Enqueue(200, Fixtures.Read("orders-list.json"), r => r.Headers.Add("X-Request-Id", "hdr-1"));

        var page = await harness.Client.Orders.ListAsync();

        Assert.Equal("08bd4a65-79dd-46ee-8ee9-e6806313b740", page.RequestId);
        Assert.Equal("hdr-1", page.Response!.RequestIdHeader);
        Assert.Equal(200, page.Response.HttpStatus);
        Assert.Equal(1, page.Response.Attempt);
        Assert.True(Guid.TryParse(page.Response.CorrelationId, out _));
    }

    [Fact]
    public async Task Falls_back_to_the_request_id_header()
    {
        using var harness = new Harness();
        harness.Handler.Enqueue(200, """{"success":true,"code":"SUCCESS","data":{"all":3}}""", r =>
        {
            r.Headers.Add("X-Request-Id", "hdr-2");
            r.Headers.Add("X-Transaction-Id", "txn-2");
        });

        var counts = await harness.Client.Orders.CountsAsync();

        Assert.Equal("hdr-2", counts.RequestId);
        Assert.Equal("txn-2", counts.Response!.TransactionId);
        Assert.Equal(3, counts.All);
    }

    [Fact]
    public async Task Flags_an_idempotent_replay()
    {
        using var harness = new Harness();
        harness.Handler.Enqueue(201, Fixtures.SessionCreated, r => r.Headers.Add("Idempotent-Replay", "true"));

        var session = await harness.Client.Checkout.CreateSessionAsync(Baskets.ThreeLine());

        Assert.True(session.Replayed);
    }

    [Fact]
    public async Task Is_not_replayed_by_default()
    {
        using var harness = new Harness();
        harness.Handler.Enqueue(201, Fixtures.SessionCreated);

        var session = await harness.Client.Checkout.CreateSessionAsync(Baskets.ThreeLine());

        Assert.False(session.Replayed);
    }

    [Fact]
    public async Task A_2xx_with_success_false_is_an_error_with_the_envelope_code()
    {
        using var harness = new Harness();
        harness.Handler.Enqueue(200, Fixtures.ValidationError);

        var ex = await Assert.ThrowsAsync<ValidationException>(() => harness.Client.Orders.CountsAsync());

        Assert.Equal("VALIDATION_ERROR", ex.Code);
        Assert.Equal(200, ex.HttpStatus);
        Assert.Equal(2, ex.Errors.Count);
        Assert.Equal("TotalAmount", ex.Errors[0].Field);
    }

    [Fact]
    public async Task A_2xx_with_success_false_and_an_unknown_code_is_an_api_error()
    {
        using var harness = new Harness();
        harness.Handler.Enqueue(200, Fixtures.Envelope("SOMETHING_ODD", null, success: false, message: "odd"));

        var ex = await Assert.ThrowsAsync<ApiException>(() => harness.Client.Orders.CountsAsync());

        Assert.Equal("SOMETHING_ODD", ex.Code);
        Assert.Equal("odd", ex.Message);
        Assert.Equal("4d51526b-617e-4c37-a3f3-20f209d87b69", ex.RequestId);
        Assert.NotNull(ex.RawEnvelope);
    }

    [Fact]
    public async Task Accepts_the_isSuccess_spelling()
    {
        using var harness = new Harness();
        harness.Handler.Enqueue(200, """{"isSuccess":false,"code":"ORDER_DECLINED","message":"no","data":{"installmentId":"o-1"}}""");

        var ex = await Assert.ThrowsAsync<OrderDeclinedException>(() => harness.Client.Orders.CountsAsync());

        Assert.Equal("o-1", ex.OrderId);
    }

    [Fact]
    public async Task A_2xx_whose_envelope_has_no_flag_is_a_success()
    {
        using var harness = new Harness();
        harness.Handler.Enqueue(200, """{"code":"SUCCESS","data":{"all":9}}""");

        var counts = await harness.Client.Orders.CountsAsync();

        Assert.Equal(9, counts.All);
    }

    [Fact]
    public async Task A_2xx_that_is_not_json_on_a_json_endpoint_is_an_api_error()
    {
        using var harness = new Harness();
        harness.Handler.Enqueue(200, "<html>proxy</html>", contentType: "text/html");

        var ex = await Assert.ThrowsAsync<ApiException>(() => harness.Client.Orders.CountsAsync());

        Assert.Equal(200, ex.HttpStatus);
        Assert.Null(ex.Code);
    }

    [Fact]
    public async Task A_2xx_with_malformed_json_is_an_api_error()
    {
        using var harness = new Harness();
        harness.Handler.Enqueue(200, "{\"success\": tru");

        var ex = await Assert.ThrowsAsync<ApiException>(() => harness.Client.Orders.CountsAsync());

        Assert.Equal(200, ex.HttpStatus);
    }

    [Fact]
    public async Task Keeps_the_decoded_data_as_raw()
    {
        using var harness = new Harness();
        harness.Handler.Enqueue(200, Fixtures.Read("order-detail.declined.json"));

        var order = await harness.Client.Orders.GetAsync("5c4a3eb5-c2b1-4d0f-b32a-062c379cfeac");

        Assert.Equal(JsonValueKind.Object, order.Raw.ValueKind);
        Assert.Equal("CART-101412-9299-G", order.Raw.GetProperty("orderReferenceId").GetString());
        Assert.Equal("DecisionDeclined", order.Raw.GetProperty("statusHistory")[2].GetProperty("trigger").GetString());
    }

    [Fact]
    public void Parses_string_errors_and_ignores_unknown_shapes()
    {
        var envelope = ApiEnvelope.TryParse("""{"success":false,"code":"X","errors":["plain",{"field":"A","message":"m"},7]}"""u8.ToArray());

        Assert.NotNull(envelope);
        Assert.False(envelope!.Success);
        Assert.Equal(2, envelope.Errors.Count);
        Assert.Null(envelope.Errors[0].Field);
        Assert.Equal("plain", envelope.Errors[0].Message);
        Assert.Equal("A", envelope.Errors[1].Field);
        Assert.Equal(JsonValueKind.Undefined, envelope.Data.ValueKind);
    }
}
