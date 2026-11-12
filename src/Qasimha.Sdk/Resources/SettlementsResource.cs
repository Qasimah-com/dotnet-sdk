using System.Globalization;
using Qasimha.Sdk.Http;

namespace Qasimha.Sdk;

/// <summary>Settlements under <c>/merchant/settlements</c>. Both calls require the <c>settlements:read</c> scope.</summary>
public sealed class SettlementsResource
{
    private readonly QasimhaTransport _transport;

    internal SettlementsResource(QasimhaTransport transport)
    {
        _transport = transport;
    }

    /// <summary>Settlement batches paid to you, newest first.</summary>
    /// <param name="request">Paging; optional.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <exception cref="PermissionException">The key lacks <c>settlements:read</c>.</exception>
    public async Task<Page<Settlement>> ListAsync(ListSettlementsRequest? request = null, CancellationToken cancellationToken = default)
    {
        var query = new List<KeyValuePair<string, string>>();
        if (request?.Page is { } page)
        {
            query.Add(new("page", page.ToString(CultureInfo.InvariantCulture)));
        }

        if (request?.PageSize is { } pageSize)
        {
            query.Add(new("pageSize", pageSize.ToString(CultureInfo.InvariantCulture)));
        }

        var response = await _transport
            .SendAsync(new ApiRequest(HttpMethod.Get, "/merchant/settlements") { Query = query }, cancellationToken)
            .ConfigureAwait(false);
        return Materializer.PageOf<Settlement>(response, request?.Page, request?.PageSize);
    }

    /// <summary>Headline figures, including the balance captured but not yet settled.</summary>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <exception cref="PermissionException">The key lacks <c>settlements:read</c>.</exception>
    public async Task<SettlementOverview> OverviewAsync(CancellationToken cancellationToken = default)
    {
        var response = await _transport
            .SendAsync(new ApiRequest(HttpMethod.Get, "/merchant/settlements/overview"), cancellationToken)
            .ConfigureAwait(false);
        return Materializer.Model<SettlementOverview>(response);
    }
}
