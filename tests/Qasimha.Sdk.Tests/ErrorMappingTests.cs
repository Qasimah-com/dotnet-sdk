using System.Net.Sockets;
using Qasimha.Sdk.Tests.Support;
using Xunit;

namespace Qasimha.Sdk.Tests;

public class ErrorMappingTests
{
    [Fact]
    public async Task A_400_is_a_validation_error_with_field_errors()
    {
        using var harness = new Harness();
        harness.Handler.Enqueue(400, Fixtures.ValidationError);

        var ex = await Assert.ThrowsAsync<ValidationException>(() => harness.Client.Checkout.CreateSessionAsync(Baskets.ThreeLine()));

        Assert.Equal(400, ex.HttpStatus);
        Assert.Equal("VALIDATION_ERROR", ex.Code);
        Assert.Equal("Validation failed", ex.Message);
        Assert.Equal("44444444-4444-4444-8444-444444444444", ex.RequestId);
        Assert.Collection(
            ex.Errors,
            e => Assert.Equal("TotalAmount", e.Field),
            e => Assert.Equal("Items[0].Quantity", e.Field));
        Assert.Single(harness.Handler.Requests);
    }

    [Fact]
    public async Task Any_other_400_code_is_still_a_validation_error()
    {
        using var harness = new Harness();
        harness.Handler.Enqueue(400, Fixtures.Envelope("INVALID_STATUS", null, success: false, message: "Cannot capture"));

        var ex = await Assert.ThrowsAsync<ValidationException>(() => harness.Client.Orders.CaptureAsync("o-1"));

        Assert.Equal("INVALID_STATUS", ex.Code);
        Assert.Empty(ex.Errors);
    }

    [Fact]
    public async Task A_401_with_an_empty_body_is_an_authentication_error()
    {
        using var harness = new Harness();
        harness.Handler.Enqueue(401, null, r => r.Headers.Add("WWW-Authenticate", "ApiKey realm=\"merchant-api\""));

        var ex = await Assert.ThrowsAsync<AuthenticationException>(() => harness.Client.Orders.CountsAsync());

        Assert.Equal(401, ex.HttpStatus);
        Assert.Null(ex.Code);
        Assert.Null(ex.RawEnvelope);
        Assert.Empty(ex.Errors);
        Assert.Contains("401", ex.Message, StringComparison.Ordinal);
        Assert.DoesNotContain(Harness.ApiKey, ex.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_403_with_an_empty_body_is_a_permission_error()
    {
        using var harness = new Harness();
        harness.Handler.Enqueue(403, null, r => r.Headers.Add("WWW-Authenticate", "ApiKey realm=\"merchant-api\""));

        var ex = await Assert.ThrowsAsync<PermissionException>(() => harness.Client.Settlements.OverviewAsync());

        Assert.Equal(403, ex.HttpStatus);
        Assert.Null(ex.Code);
    }

    [Fact]
    public async Task A_403_with_an_envelope_keeps_its_code()
    {
        using var harness = new Harness();
        harness.Handler.Enqueue(403, Fixtures.Envelope("FORBIDDEN", null, success: false, message: "Missing scope orders:write"));

        var ex = await Assert.ThrowsAsync<PermissionException>(() => harness.Client.Orders.RefundAsync("o-1", new RefundRequest { Amount = 1m }));

        Assert.Equal("FORBIDDEN", ex.Code);
        Assert.Equal("Missing scope orders:write", ex.Message);
    }

    [Fact]
    public async Task A_404_is_not_found()
    {
        using var harness = new Harness();
        harness.Handler.Enqueue(404, Fixtures.Envelope("NOT_FOUND", null, success: false, message: "Order not found"));

        var ex = await Assert.ThrowsAsync<NotFoundException>(() => harness.Client.Orders.GetAsync("missing"));

        Assert.Equal("NOT_FOUND", ex.Code);
    }

    [Fact]
    public async Task A_409_duplicate_on_the_first_attempt_carries_the_existing_order()
    {
        using var harness = new Harness();
        harness.Handler.Enqueue(409, Fixtures.Duplicate);

        var ex = await Assert.ThrowsAsync<DuplicateOrderReferenceException>(() => harness.Client.Checkout.CreateSessionAsync(Baskets.ThreeLine()));

        Assert.Equal("9a2e6c4b-1111-4c2d-8a1f-000000000001", ex.ExistingOrderId);
        Assert.Equal("https://localhost/checkout/abc", ex.ExistingPaymentUrl);
        Assert.Equal(409, ex.HttpStatus);
        Assert.Single(harness.Handler.Requests);
    }

    [Fact]
    public async Task A_409_duplicate_without_data_has_no_existing_order()
    {
        using var harness = new Harness();
        harness.Handler.Enqueue(409, Fixtures.DuplicateWithoutOrder);

        var ex = await Assert.ThrowsAsync<DuplicateOrderReferenceException>(() => harness.Client.Checkout.CreateSessionAsync(Baskets.ThreeLine()));

        Assert.Null(ex.ExistingOrderId);
        Assert.Null(ex.ExistingPaymentUrl);
    }

    [Fact]
    public async Task Any_other_409_is_a_conflict()
    {
        using var harness = new Harness();
        harness.Handler.Enqueue(409, Fixtures.Envelope("MERCHANT_SUSPENDED", null, success: false, message: "Suspended"));

        var ex = await Assert.ThrowsAsync<ConflictException>(() => harness.Client.Checkout.CreateSessionAsync(Baskets.ThreeLine()));

        Assert.Equal("MERCHANT_SUSPENDED", ex.Code);
        Assert.Single(harness.Handler.Requests);
    }

    [Fact]
    public async Task A_422_declined_carries_the_order_id()
    {
        using var harness = new Harness();
        harness.Handler.Enqueue(422, Fixtures.Declined);

        var ex = await Assert.ThrowsAsync<OrderDeclinedException>(() => harness.Client.Checkout.CreateSessionAsync(Baskets.ThreeLine()));

        Assert.Equal("76c94cf2-db05-4c5a-b8da-bb05d1ae7cbc", ex.OrderId);
        Assert.Equal("ORDER_DECLINED", ex.Code);
        Assert.Equal(422, ex.HttpStatus);
    }

    [Fact]
    public async Task Any_other_422_is_an_api_error()
    {
        using var harness = new Harness();
        harness.Handler.Enqueue(422, Fixtures.Envelope("UNPROCESSABLE", null, success: false));

        var ex = await Assert.ThrowsAsync<ApiException>(() => harness.Client.Orders.CountsAsync());

        Assert.Equal("UNPROCESSABLE", ex.Code);
        Assert.IsNotType<ServerException>(ex);
    }

    [Fact]
    public async Task A_429_after_the_retries_is_a_rate_limit_error_with_retry_after()
    {
        using var harness = new Harness(o => o.MaxRetries = 0);
        harness.Handler.Enqueue(429, Fixtures.Envelope("RATE_LIMITED", null, success: false), r => r.Headers.Add("Retry-After", "7"));

        var ex = await Assert.ThrowsAsync<RateLimitException>(() => harness.Client.Orders.CountsAsync());

        Assert.Equal(TimeSpan.FromSeconds(7), ex.RetryAfter);
        Assert.Equal(429, ex.HttpStatus);
    }

    [Fact]
    public async Task A_500_is_a_server_error_and_is_never_retried()
    {
        using var harness = new Harness();
        harness.Handler.Enqueue(500, Fixtures.Envelope("INTERNAL", null, success: false, message: "boom"));

        var ex = await Assert.ThrowsAsync<ServerException>(() => harness.Client.Checkout.CreateSessionAsync(Baskets.ThreeLine()));

        Assert.Equal(500, ex.HttpStatus);
        Assert.IsAssignableFrom<ApiException>(ex);
        Assert.Single(harness.Handler.Requests);
        Assert.Empty(harness.Delay.Waits);
    }

    [Fact]
    public async Task A_500_without_a_body_has_a_fixed_message()
    {
        using var harness = new Harness();
        harness.Handler.Enqueue(500);

        var ex = await Assert.ThrowsAsync<ServerException>(() => harness.Client.Orders.CountsAsync());

        Assert.StartsWith("HTTP 500", ex.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(302)]
    [InlineData(402)]
    [InlineData(418)]
    public async Task Any_other_status_is_an_api_error(int status)
    {
        using var harness = new Harness();
        harness.Handler.Enqueue(status, null, r => r.Headers.TryAddWithoutValidation("Location", "https://localhost/elsewhere"));

        var ex = await Assert.ThrowsAsync<ApiException>(() => harness.Client.Orders.CountsAsync());

        Assert.Equal(status, ex.HttpStatus);
        Assert.Single(harness.Handler.Requests);
    }

    [Fact]
    public async Task A_transport_failure_is_a_connection_error_wrapping_the_cause()
    {
        using var harness = new Harness(o => o.MaxRetries = 0);
        var cause = new HttpRequestException("No such host is known.", new SocketException(11001));
        harness.Handler.EnqueueThrow(cause);

        var ex = await Assert.ThrowsAsync<ConnectionException>(() => harness.Client.Orders.CountsAsync());

        Assert.Same(cause, ex.InnerException);
        Assert.Null(ex.HttpStatus);
        Assert.Null(ex.Code);
        Assert.Contains("No such host", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_timeout_is_a_connection_error()
    {
        using var harness = new Harness(o =>
        {
            o.MaxRetries = 0;
            o.Timeout = TimeSpan.FromMilliseconds(50);
        });
        harness.Handler.EnqueueHang();

        var ex = await Assert.ThrowsAsync<ConnectionException>(() => harness.Client.Orders.CountsAsync());

        Assert.Contains("timed out", ex.Message, StringComparison.Ordinal);
        Assert.IsAssignableFrom<OperationCanceledException>(ex.InnerException);
    }

    [Fact]
    public async Task Caller_cancellation_is_not_wrapped()
    {
        using var harness = new Harness();
        harness.Handler.EnqueueHang();
        using var cts = new CancellationTokenSource();
        cts.CancelAfter(50);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => harness.Client.Orders.CountsAsync(cts.Token));
    }

    [Fact]
    public async Task Passes_the_api_message_through_unchanged()
    {
        using var harness = new Harness(o => o.Language = "ar");
        harness.Handler.Enqueue(400, Fixtures.Envelope("INVALID_AMOUNT", null, success: false, message: "المبلغ غير صالح"));

        var ex = await Assert.ThrowsAsync<ValidationException>(() => harness.Client.Orders.RefundAsync("o-1", new RefundRequest { Amount = 1m }));

        Assert.Equal("المبلغ غير صالح", ex.Message);
    }

    [Fact]
    public void Exceptions_can_be_constructed_by_hand()
    {
        var plain = new ValidationException("bad");
        Assert.Null(plain.Code);
        Assert.Empty(plain.Errors);

        var withErrors = new ValidationException("bad", new[] { new ApiFieldError("A", "m") });
        Assert.Single(withErrors.Errors);

        var rich = new RateLimitException("slow", new ApiErrorDetails("RL", 429, "req", null, null), TimeSpan.FromSeconds(3));
        Assert.Equal("RL", rich.Code);
        Assert.Equal(429, rich.HttpStatus);
        Assert.Equal("req", rich.RequestId);
        Assert.Equal(TimeSpan.FromSeconds(3), rich.RetryAfter);

        var connection = new ConnectionException("down", new IOException("reset"));
        Assert.IsType<IOException>(connection.InnerException);
    }
}
