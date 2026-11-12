using Qasimha.Sdk.Http;
using Qasimha.Sdk.Validation;

namespace Qasimha.Sdk;

/// <summary>Payment links and QR orders under <c>/merchant/checkout-links</c>.</summary>
public sealed class CheckoutLinksResource
{
    private readonly QasimhaTransport _transport;

    internal CheckoutLinksResource(QasimhaTransport transport)
    {
        _transport = transport;
    }

    /// <summary>
    /// Creates an order up-front and returns the hosted checkout URL, sent by SMS for the <c>Link</c>
    /// channel or rendered by you as a QR code for the <c>Qr</c> channel. Links expire after two hours.
    /// </summary>
    /// <param name="request">The link; validated before sending.</param>
    /// <param name="options">Per-call idempotency options.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <exception cref="ValidationException">A <c>Link</c> without a phone (nothing sent), or a 400 from the API.</exception>
    /// <exception cref="DuplicateOrderReferenceException">The reference is already in use.</exception>
    /// <exception cref="ConflictException"><c>MERCHANT_SUSPENDED</c>.</exception>
    public async Task<CheckoutLink> CreateAsync(
        CreateCheckoutLinkRequest request,
        RequestOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        RequestValidator.Validate(request);

        const string path = "/merchant/checkout-links";
        try
        {
            var response = await _transport
                .SendAsync(new ApiRequest(HttpMethod.Post, path) { Body = request, Options = options }, cancellationToken)
                .ConfigureAwait(false);
            return Materializer.Model<CheckoutLink>(response);
        }
        catch (DuplicateOrderReferenceException ex) when (ReplayedCreate.Matches(ex))
        {
            ReplayedCreate.Log(_transport, ex, path);
            return Materializer.FromElement<CheckoutLink>(ex.RawEnvelope!.Data, ex.Response);
        }
    }

    /// <summary>Your 50 most recent links, each with a <see cref="CheckoutLink.Waiting"/> flag.</summary>
    /// <param name="cancellationToken">Cancels the call.</param>
    public async Task<ApiList<CheckoutLink>> ListAsync(CancellationToken cancellationToken = default)
    {
        var response = await _transport
            .SendAsync(new ApiRequest(HttpMethod.Get, "/merchant/checkout-links"), cancellationToken)
            .ConfigureAwait(false);
        return Materializer.ListOf<CheckoutLink>(response);
    }

    /// <summary>Polls one link. Expired links flip to <see cref="OrderStatus.Expired"/> on read.</summary>
    /// <param name="id">The link (order) id.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <exception cref="NotFoundException">The link does not exist or is not yours.</exception>
    public async Task<CheckoutLink> GetAsync(string id, CancellationToken cancellationToken = default)
    {
        var response = await _transport
            .SendAsync(new ApiRequest(HttpMethod.Get, "/merchant/checkout-links/" + Escape(id)), cancellationToken)
            .ConfigureAwait(false);
        return Materializer.Model<CheckoutLink>(response);
    }

    /// <summary>Cancels a link that is still open for checkout.</summary>
    /// <param name="id">The link (order) id.</param>
    /// <param name="options">Per-call idempotency options.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <exception cref="ConflictException"><c>NOT_OPEN</c>: the customer already completed checkout or the link expired.</exception>
    public async Task<CheckoutLink> CancelAsync(string id, RequestOptions? options = null, CancellationToken cancellationToken = default)
    {
        var response = await _transport
            .SendAsync(
                new ApiRequest(HttpMethod.Post, "/merchant/checkout-links/" + Escape(id) + "/cancel") { Options = options },
                cancellationToken)
            .ConfigureAwait(false);
        return Materializer.Model<CheckoutLink>(response);
    }

    private static string Escape(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("A checkout link id is required.", nameof(id));
        }

        return Uri.EscapeDataString(id.Trim());
    }
}
