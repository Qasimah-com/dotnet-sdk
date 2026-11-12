using System.Globalization;
using Qasimha.Sdk.Http;
using Qasimha.Sdk.Validation;

namespace Qasimha.Sdk;

/// <summary>
/// Your webhook endpoints under <c>/merchant/webhooks</c>: register, pause, rotate secrets, send a test
/// event and read the delivery log. Every method requires the <c>webhooks:manage</c> scope; a key
/// without it gets <see cref="PermissionException"/>. Verification of deliveries is
/// <see cref="WebhooksResource"/>.
/// </summary>
public sealed class WebhookEndpointsResource
{
    private readonly QasimhaTransport _transport;

    internal WebhookEndpointsResource(QasimhaTransport transport)
    {
        _transport = transport;
    }

    /// <summary>
    /// Registers an HTTPS URL and returns its signing secret <em>once</em>. Store
    /// <see cref="WebhookEndpointWithSecret.Secret"/> in a secrets manager; it cannot be read back.
    /// </summary>
    /// <param name="request">The endpoint; the URL and event names are checked before sending.</param>
    /// <param name="options">Per-call idempotency options.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <exception cref="ValidationException">A non-<c>https</c> URL or an unknown event name (nothing sent), or a 400 from the API: <c>URL_INVALID</c>, <c>URL_PRIVATE</c>, <c>URL_TOO_LONG</c>, <c>INVALID_EVENT</c>, <c>LIMIT_EXCEEDED</c> (5 endpoints per account).</exception>
    public async Task<WebhookEndpointWithSecret> CreateAsync(
        CreateWebhookEndpointRequest request,
        RequestOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        RequestValidator.Validate(request);
        var response = await _transport
            .SendAsync(new ApiRequest(HttpMethod.Post, "/merchant/webhooks") { Body = request, Options = options }, cancellationToken)
            .ConfigureAwait(false);
        return Materializer.Model<WebhookEndpointWithSecret>(response);
    }

    /// <summary>Every endpoint registered for your account, newest first.</summary>
    /// <param name="cancellationToken">Cancels the call.</param>
    public async Task<ApiList<WebhookEndpoint>> ListAsync(CancellationToken cancellationToken = default)
    {
        var response = await _transport
            .SendAsync(new ApiRequest(HttpMethod.Get, "/merchant/webhooks"), cancellationToken)
            .ConfigureAwait(false);
        return Materializer.ListOf<WebhookEndpoint>(response);
    }

    /// <summary>Reads one endpoint.</summary>
    /// <param name="id">The endpoint id.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <exception cref="NotFoundException">The endpoint does not exist or is not yours.</exception>
    public async Task<WebhookEndpoint> GetAsync(string id, CancellationToken cancellationToken = default)
    {
        var response = await _transport
            .SendAsync(new ApiRequest(HttpMethod.Get, "/merchant/webhooks/" + EscapeEndpointId(id)), cancellationToken)
            .ConfigureAwait(false);
        return Materializer.Model<WebhookEndpoint>(response);
    }

    /// <summary>Changes the URL, description, subscribed events or active flag. Unset members keep their value. Sent once, without retries.</summary>
    /// <param name="id">The endpoint id.</param>
    /// <param name="request">The changes; the URL and event names are checked before sending.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <exception cref="ValidationException">A non-<c>https</c> URL or an unknown event name (nothing sent), or a 400 from the API.</exception>
    /// <exception cref="NotFoundException">The endpoint does not exist or is not yours.</exception>
    public async Task<WebhookEndpoint> UpdateAsync(string id, UpdateWebhookEndpointRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        RequestValidator.Validate(request);
        var response = await _transport
            .SendAsync(new ApiRequest(HttpMethod.Put, "/merchant/webhooks/" + EscapeEndpointId(id)) { Body = request }, cancellationToken)
            .ConfigureAwait(false);
        return Materializer.Model<WebhookEndpoint>(response);
    }

    /// <summary>Deletes an endpoint. Deliveries still waiting for a retry end as failed. Sent once, without retries.</summary>
    /// <param name="id">The endpoint id.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <exception cref="NotFoundException">The endpoint does not exist or is not yours.</exception>
    public async Task DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        await _transport
            .SendAsync(new ApiRequest(HttpMethod.Delete, "/merchant/webhooks/" + EscapeEndpointId(id)), cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Issues a new secret and returns it <em>once</em>. Until <see cref="WebhookEndpoint.PreviousSecretExpiresAt"/>
    /// every delivery is signed with both secrets (two <c>v1</c> values), so verify with either during that window.
    /// </summary>
    /// <param name="id">The endpoint id.</param>
    /// <param name="options">Per-call idempotency options.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <exception cref="NotFoundException">The endpoint does not exist or is not yours.</exception>
    public async Task<WebhookEndpointWithSecret> RotateSecretAsync(string id, RequestOptions? options = null, CancellationToken cancellationToken = default)
    {
        var response = await _transport
            .SendAsync(
                new ApiRequest(HttpMethod.Post, "/merchant/webhooks/" + EscapeEndpointId(id) + "/rotate-secret") { Options = options },
                cancellationToken)
            .ConfigureAwait(false);
        return Materializer.Model<WebhookEndpointWithSecret>(response);
    }

    /// <summary>Queues a signed <c>ping</c> event to the endpoint. Watch it arrive in <see cref="ListDeliveriesAsync"/>.</summary>
    /// <param name="id">The endpoint id.</param>
    /// <param name="options">Per-call idempotency options.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <exception cref="NotFoundException">The endpoint does not exist or is not yours.</exception>
    public async Task<WebhookPingResult> PingAsync(string id, RequestOptions? options = null, CancellationToken cancellationToken = default)
    {
        var response = await _transport
            .SendAsync(
                new ApiRequest(HttpMethod.Post, "/merchant/webhooks/" + EscapeEndpointId(id) + "/ping") { Options = options },
                cancellationToken)
            .ConfigureAwait(false);
        return Materializer.Model<WebhookPingResult>(response);
    }

    /// <summary>The endpoint's delivery log, newest first.</summary>
    /// <param name="id">The endpoint id.</param>
    /// <param name="request">Status filter and paging; all optional.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <exception cref="NotFoundException">The endpoint does not exist or is not yours.</exception>
    public async Task<Page<WebhookDelivery>> ListDeliveriesAsync(
        string id,
        ListWebhookDeliveriesRequest? request = null,
        CancellationToken cancellationToken = default)
    {
        var path = "/merchant/webhooks/" + EscapeEndpointId(id) + "/deliveries";
        var query = new List<KeyValuePair<string, string>>();
        if (!string.IsNullOrWhiteSpace(request?.Status))
        {
            query.Add(new("status", request.Status));
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
            .SendAsync(new ApiRequest(HttpMethod.Get, path) { Query = query }, cancellationToken)
            .ConfigureAwait(false);
        return Materializer.PageOf<WebhookDelivery>(response, request?.Page, request?.PageSize);
    }

    /// <summary>Queues the same event to the endpoint again as a new delivery; the original row is kept.</summary>
    /// <param name="id">The endpoint id.</param>
    /// <param name="deliveryId">The delivery to replay.</param>
    /// <param name="options">Per-call idempotency options.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <exception cref="NotFoundException">The endpoint or the delivery does not exist or is not yours.</exception>
    public async Task<WebhookDelivery> ReplayDeliveryAsync(
        string id,
        string deliveryId,
        RequestOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var path = "/merchant/webhooks/" + EscapeEndpointId(id) + "/deliveries/" + Escape(deliveryId, "A delivery id is required.", nameof(deliveryId)) + "/replay";
        var response = await _transport
            .SendAsync(new ApiRequest(HttpMethod.Post, path) { Options = options }, cancellationToken)
            .ConfigureAwait(false);
        return Materializer.Model<WebhookDelivery>(response);
    }

    private static string EscapeEndpointId(string id) => Escape(id, "A webhook endpoint id is required.", nameof(id));

    private static string Escape(string value, string message, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(message, parameterName);
        }

        return Uri.EscapeDataString(value.Trim());
    }
}
