using System.Globalization;
using Qasimha.Sdk.Http;
using Qasimha.Sdk.Validation;

namespace Qasimha.Sdk;

/// <summary>
/// Orders under <c>/merchant/orders</c>, read by Qasimha id (<see cref="GetAsync"/>) or by your own
/// reference (<see cref="GetByReferenceAsync"/>). An order that completes the hosted checkout is already
/// <see cref="OrderStatus.Confirmed"/> (first instalment paid): mark it paid, call
/// <see cref="FulfillAsync"/> on shipment and <see cref="RefundAsync"/> after that.
/// <see cref="CaptureAsync"/> applies to <see cref="OrderStatus.New"/> and <see cref="OrderStatus.Approved"/> orders only.
/// </summary>
public sealed class OrdersResource
{
    private readonly QasimhaTransport _transport;

    internal OrdersResource(QasimhaTransport transport)
    {
        _transport = transport;
    }

    /// <summary>Lists your orders, newest first. Requires <c>orders:read</c>.</summary>
    /// <param name="request">Filters and paging; all optional.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    public async Task<Page<OrderSummary>> ListAsync(ListOrdersRequest? request = null, CancellationToken cancellationToken = default)
    {
        var query = new List<KeyValuePair<string, string>>();
        if (!string.IsNullOrWhiteSpace(request?.Status))
        {
            query.Add(new("status", request.Status));
        }

        if (!string.IsNullOrWhiteSpace(request?.Search))
        {
            query.Add(new("search", request.Search));
        }

        if (request?.Page is { } page)
        {
            query.Add(new("page", page.ToString(CultureInfo.InvariantCulture)));
        }

        if (request?.PageSize is { } pageSize)
        {
            query.Add(new("pageSize", pageSize.ToString(CultureInfo.InvariantCulture)));
        }

        var response = await _transport
            .SendAsync(new ApiRequest(HttpMethod.Get, "/merchant/orders") { Query = query }, cancellationToken)
            .ConfigureAwait(false);
        return Materializer.PageOf<OrderSummary>(response, request?.Page, request?.PageSize);
    }

    /// <summary>Reads one order in full. Requires <c>orders:read</c>.</summary>
    /// <param name="id">The Qasimha order id.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <exception cref="NotFoundException">The order does not exist or is not yours.</exception>
    public async Task<Order> GetAsync(string id, CancellationToken cancellationToken = default)
    {
        var response = await _transport
            .SendAsync(new ApiRequest(HttpMethod.Get, "/merchant/orders/" + Escape(id)), cancellationToken)
            .ConfigureAwait(false);
        return Materializer.Model<Order>(response);
    }

    /// <summary>
    /// Reads one order in full by the <c>OrderReferenceId</c> you sent when it was created: the lookup a
    /// webhook or a return route uses when it only holds your reference. Exact, case-sensitive match.
    /// Requires <c>orders:read</c>.
    /// </summary>
    /// <param name="orderReferenceId">Your order reference. A <c>/</c> inside it is kept as a path separator; everything else is percent-encoded.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <exception cref="ArgumentException">The reference is empty, or has a <c>.</c> or <c>..</c> segment, which no URL can carry.</exception>
    /// <exception cref="NotFoundException">No order of yours has that reference.</exception>
    public async Task<Order> GetByReferenceAsync(string orderReferenceId, CancellationToken cancellationToken = default)
    {
        var response = await _transport
            .SendAsync(new ApiRequest(HttpMethod.Get, "/merchant/orders/by-reference/" + EscapeReference(orderReferenceId)), cancellationToken)
            .ConfigureAwait(false);
        return Materializer.Model<Order>(response);
    }

    /// <summary>Confirms (captures) a <see cref="OrderStatus.New"/> or <see cref="OrderStatus.Approved"/> order. Requires <c>orders:write</c>.</summary>
    /// <param name="id">The Qasimha order id.</param>
    /// <param name="options">Per-call idempotency options.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <exception cref="ValidationException"><c>INVALID_STATUS</c>: the order cannot be captured in its state (a confirmed order already is).</exception>
    public async Task<CaptureResult> CaptureAsync(string id, RequestOptions? options = null, CancellationToken cancellationToken = default)
    {
        var response = await _transport
            .SendAsync(new ApiRequest(HttpMethod.Post, "/merchant/orders/" + Escape(id) + "/capture") { Options = options }, cancellationToken)
            .ConfigureAwait(false);
        return Materializer.Model<CaptureResult>(response);
    }

    /// <summary>Cancels (voids) an order that has not been fulfilled. Requires <c>orders:write</c>.</summary>
    /// <param name="id">The Qasimha order id.</param>
    /// <param name="options">Per-call idempotency options.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <exception cref="ValidationException"><c>INVALID_STATUS</c>: the order cannot be cancelled in its state.</exception>
    public async Task<CancelResult> CancelAsync(string id, RequestOptions? options = null, CancellationToken cancellationToken = default)
    {
        var response = await _transport
            .SendAsync(new ApiRequest(HttpMethod.Post, "/merchant/orders/" + Escape(id) + "/cancel") { Options = options }, cancellationToken)
            .ConfigureAwait(false);
        return Materializer.Model<CancelResult>(response);
    }

    /// <summary>Refunds a fulfilled order, fully or partially. Requires <c>orders:write</c>.</summary>
    /// <param name="id">The Qasimha order id.</param>
    /// <param name="request">The amount and reason.</param>
    /// <param name="options">Per-call idempotency options.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <exception cref="ValidationException"><c>INVALID_STATUS</c>, <c>INVALID_AMOUNT</c> or <c>AMOUNT_EXCEEDED</c>.</exception>
    public async Task<RefundResult> RefundAsync(string id, RefundRequest request, RequestOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        RequestValidator.Validate(request);
        var response = await _transport
            .SendAsync(
                new ApiRequest(HttpMethod.Post, "/merchant/orders/" + Escape(id) + "/refund") { Body = request, Options = options },
                cancellationToken)
            .ConfigureAwait(false);
        return Materializer.Model<RefundResult>(response);
    }

    /// <summary>Records a full or partial fulfilment when you ship. Requires <c>orders:write</c>.</summary>
    /// <param name="id">The Qasimha order id.</param>
    /// <param name="request">Shipment details; omit to fulfil everything outstanding.</param>
    /// <param name="options">Per-call idempotency options.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <exception cref="ValidationException"><c>INVALID_STATUS</c> or <c>INVALID_SHIPPED_AMOUNT</c>.</exception>
    public async Task<FulfillResult> FulfillAsync(string id, FulfillRequest? request = null, RequestOptions? options = null, CancellationToken cancellationToken = default)
    {
        request ??= new FulfillRequest();
        RequestValidator.Validate(request);
        var response = await _transport
            .SendAsync(
                new ApiRequest(HttpMethod.Post, "/merchant/orders/" + Escape(id) + "/fulfill") { Body = request, Options = options },
                cancellationToken)
            .ConfigureAwait(false);
        return Materializer.Model<FulfillResult>(response);
    }

    /// <summary>Order counts per portal tab. Requires <c>orders:read</c>.</summary>
    /// <param name="cancellationToken">Cancels the call.</param>
    public async Task<OrderCounts> CountsAsync(CancellationToken cancellationToken = default)
    {
        var response = await _transport
            .SendAsync(new ApiRequest(HttpMethod.Get, "/merchant/orders/counts"), cancellationToken)
            .ConfigureAwait(false);
        return Materializer.Model<OrderCounts>(response);
    }

    /// <summary>Downloads the order's financing contract (a PDF). Requires <c>orders:read</c>.</summary>
    /// <param name="id">The Qasimha order id.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    public async Task<OrderContract> ContractAsync(string id, CancellationToken cancellationToken = default)
    {
        var response = await _transport
            .SendAsync(
                new ApiRequest(HttpMethod.Get, "/merchant/orders/" + Escape(id) + "/contract")
                {
                    Accept = "application/pdf, application/json;q=0.9",
                },
                cancellationToken)
            .ConfigureAwait(false);
        return new OrderContract
        {
            Content = response.Body,
            ContentType = response.ContentType,
            FileName = response.FileName,
            Raw = response.Envelope?.Data ?? default,
            Response = response.Info,
        };
    }

    private static string Escape(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("An order id is required.", nameof(id));
        }

        return Uri.EscapeDataString(id.Trim());
    }

    private static string EscapeReference(string orderReferenceId)
    {
        if (string.IsNullOrWhiteSpace(orderReferenceId))
        {
            throw new ArgumentException("An order reference is required.", nameof(orderReferenceId));
        }

        var segments = orderReferenceId.Trim().Split('/');
        for (var i = 0; i < segments.Length; i++)
        {
            if (segments[i] is "." or "..")
            {
                throw new ArgumentException("An order reference cannot contain a '.' or '..' path segment.", nameof(orderReferenceId));
            }

            segments[i] = Uri.EscapeDataString(segments[i]);
        }

        return string.Join("/", segments);
    }
}
