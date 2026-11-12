using Qasimha.Sdk.Http;
using Qasimha.Sdk.Validation;

namespace Qasimha.Sdk;

/// <summary>
/// Checkout sessions: online (<c>/pay/session/create</c>), POS (<c>/pay/pos/create</c>) and QR
/// (<c>/pay/qr/create</c>), and the plan quote for an amount (<c>/widget/plans</c>).
/// </summary>
public sealed class CheckoutResource
{
    private readonly QasimhaTransport _transport;

    internal CheckoutResource(QasimhaTransport transport)
    {
        _transport = transport;
    }

    /// <summary>
    /// Creates an online payment session and returns the hosted checkout URL to redirect the shopper to.
    /// A duplicate reference met on a <em>retry</em> (the first attempt timed out after succeeding) is
    /// returned as the session already open; on the first attempt it is <see cref="DuplicateOrderReferenceException"/>.
    /// </summary>
    /// <param name="request">The basket; validated before sending.</param>
    /// <param name="options">Per-call idempotency options.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <exception cref="ValidationException">The basket is invalid (nothing sent) or the API refused it.</exception>
    /// <exception cref="DuplicateOrderReferenceException">The reference is already in use.</exception>
    /// <exception cref="OrderDeclinedException">The order was created but declined.</exception>
    public async Task<Session> CreateSessionAsync(
        CreateSessionRequest request,
        RequestOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        RequestValidator.Validate(request);

        const string path = "/pay/session/create";
        try
        {
            var response = await _transport
                .SendAsync(new ApiRequest(HttpMethod.Post, path) { Body = request, Options = options }, cancellationToken)
                .ConfigureAwait(false);
            return Materializer.Model<Session>(response);
        }
        catch (DuplicateOrderReferenceException ex) when (ReplayedCreate.Matches(ex))
        {
            ReplayedCreate.Log(_transport, ex, path);
            return Materializer.FromElement<Session>(ex.RawEnvelope!.Data, ex.Response);
        }
    }

    /// <summary>Creates an in-store order and returns the hosted checkout page for the customer to open.</summary>
    /// <param name="request">The basket; validated before sending, <see cref="Consumer.Email"/> required.</param>
    /// <param name="options">Per-call idempotency options.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <exception cref="ValidationException">The basket is invalid (nothing sent) or the API refused it.</exception>
    /// <exception cref="DuplicateOrderReferenceException">The reference is already in use.</exception>
    public async Task<PosSession> CreatePosSessionAsync(
        CreatePosSessionRequest request,
        RequestOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        RequestValidator.Validate(request);

        const string path = "/pay/pos/create";
        try
        {
            var response = await _transport
                .SendAsync(new ApiRequest(HttpMethod.Post, path) { Body = request, Options = options }, cancellationToken)
                .ConfigureAwait(false);
            return Materializer.Model<PosSession>(response);
        }
        catch (DuplicateOrderReferenceException ex) when (ReplayedCreate.Matches(ex))
        {
            ReplayedCreate.Log(_transport, ex, path);
            return Materializer.FromElement<PosSession>(ex.RawEnvelope!.Data, ex.Response);
        }
    }

    /// <summary>
    /// Creates a scan-to-pay session and returns the URL to render as a QR code. A declined QR order
    /// (a 201 with <c>status: "Declined"</c> and no URL today) is raised as <see cref="OrderDeclinedException"/>,
    /// the same outcome as the other two endpoints.
    /// </summary>
    /// <param name="request">The basket; validated before sending.</param>
    /// <param name="options">Per-call idempotency options.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <exception cref="ValidationException">The basket is invalid (nothing sent) or the API refused it.</exception>
    /// <exception cref="DuplicateOrderReferenceException">The reference is already in use.</exception>
    /// <exception cref="OrderDeclinedException">The order was created but declined.</exception>
    public async Task<QrSession> CreateQrSessionAsync(
        CreateQrSessionRequest request,
        RequestOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        RequestValidator.Validate(request);

        const string path = "/pay/qr/create";
        QrSession session;
        try
        {
            var response = await _transport
                .SendAsync(new ApiRequest(HttpMethod.Post, path) { Body = request, Options = options }, cancellationToken)
                .ConfigureAwait(false);
            session = Materializer.Model<QrSession>(response);
        }
        catch (DuplicateOrderReferenceException ex) when (ReplayedCreate.Matches(ex))
        {
            ReplayedCreate.Log(_transport, ex, path);
            session = Materializer.FromElement<QrSession>(ex.RawEnvelope!.Data, ex.Response);
        }

        if (string.Equals(session.Status, OrderStatus.Declined, StringComparison.Ordinal)
            && string.IsNullOrEmpty(session.QrCodeUrl))
        {
            var info = session.Response;
            throw new OrderDeclinedException(
                "The order was declined.",
                new ApiErrorDetails(ErrorMapper.OrderDeclined, info?.HttpStatus, info?.RequestId, null, null),
                session.OrderId)
            {
                Response = info,
            };
        }

        return session;
    }

    /// <summary>
    /// Quotes the instalment plans your policy offers for <paramref name="amount"/>: what the on-site
    /// widget shows under a price, with no customer data. Authenticates with the publishable key in
    /// <c>X-Public-Key</c>; the API key is not sent. <see cref="Plans.Items"/> is empty when the amount
    /// is outside <c>[MinAmount, MaxAmount]</c> or the account has no active policy.
    /// </summary>
    /// <param name="amount">The basket or product amount in SAR, at most two decimals.</param>
    /// <param name="options">Per-call language and publishable key; both default to the client's.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <exception cref="ConfigurationException">No publishable key is available, or the per-call one is malformed or from the other environment; nothing is sent.</exception>
    /// <exception cref="ValidationException">The amount is negative or has more than two decimals (nothing sent), or the API refused it (<c>INVALID_AMOUNT</c>, <c>UNSUPPORTED_CURRENCY</c>).</exception>
    /// <exception cref="AuthenticationException">The publishable key was rejected (<c>PUBLIC_KEY_REQUIRED</c>, <c>INVALID_PUBLIC_KEY</c>).</exception>
    public async Task<Plans> GetPlansAsync(decimal amount, GetPlansOptions? options = null, CancellationToken cancellationToken = default)
    {
        var configuration = _transport.Configuration;
        var publicKey = configuration.ResolvePublishableKey(options?.PublishableKey);
        var language = string.IsNullOrWhiteSpace(options?.Lang) ? null : ClientConfiguration.NormalizeLanguage(options!.Lang);
        RequestValidator.ValidateAmount("Amount", amount);

        var query = new List<KeyValuePair<string, string>>
        {
            new("amount", Money.Format(amount)),
            new("currency", "SAR"),
            new("lang", language ?? configuration.Language),
        };

        var response = await _transport
            .SendAsync(
                new ApiRequest(HttpMethod.Get, "/widget/plans") { Query = query, PublicKey = publicKey, Language = language },
                cancellationToken)
            .ConfigureAwait(false);
        return Materializer.Model<Plans>(response);
    }
}
