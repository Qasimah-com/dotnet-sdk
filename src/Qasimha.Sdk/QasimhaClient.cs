using Qasimha.Sdk.Http;

namespace Qasimha.Sdk;

/// <summary>
/// Entry point to the Qasimha merchant API. Thread-safe: create one per API key and reuse it for
/// the lifetime of the application.
/// </summary>
public sealed class QasimhaClient : IDisposable
{
    private readonly HttpClient _http;
    private readonly bool _ownsHttpClient;
    private readonly ClientConfiguration _config;

    /// <summary>Creates a client that owns its HTTP connection pool (redirects disabled, runtime proxy settings honoured).</summary>
    /// <param name="options">Client settings; validated here.</param>
    /// <exception cref="ConfigurationException">The options are invalid.</exception>
    public QasimhaClient(QasimhaClientOptions options)
        : this(options, httpClient: null, handler: null, disposeHandler: true)
    {
    }

    /// <summary>
    /// Creates a client over your own <see cref="HttpMessageHandler"/>. Configure it with
    /// <c>AllowAutoRedirect = false</c>; the SDK never follows redirects.
    /// </summary>
    /// <param name="options">Client settings; validated here.</param>
    /// <param name="handler">The handler every request goes through.</param>
    /// <param name="disposeHandler">Whether <see cref="Dispose"/> disposes <paramref name="handler"/>.</param>
    /// <exception cref="ConfigurationException">The options are invalid.</exception>
    public QasimhaClient(QasimhaClientOptions options, HttpMessageHandler handler, bool disposeHandler = false)
        : this(options, httpClient: null, handler ?? throw new ArgumentNullException(nameof(handler)), disposeHandler)
    {
    }

    /// <summary>
    /// Creates a client over an <see cref="HttpClient"/> you manage, typically from
    /// <c>IHttpClientFactory</c>. The SDK does not dispose it. Its primary handler should have
    /// <c>AllowAutoRedirect = false</c>; the SDK never follows redirects.
    /// </summary>
    /// <param name="options">Client settings; validated here.</param>
    /// <param name="httpClient">The client every request goes through.</param>
    /// <exception cref="ConfigurationException">The options are invalid.</exception>
    public QasimhaClient(QasimhaClientOptions options, HttpClient httpClient)
        : this(options, httpClient ?? throw new ArgumentNullException(nameof(httpClient)), handler: null, disposeHandler: false)
    {
    }

    private QasimhaClient(QasimhaClientOptions options, HttpClient? httpClient, HttpMessageHandler? handler, bool disposeHandler)
    {
        _config = ClientConfiguration.From(options);

        if (httpClient is not null)
        {
            _http = httpClient;
            _ownsHttpClient = false;
        }
        else
        {
            _http = new HttpClient(handler ?? CreateDefaultHandler(), disposeHandler) { Timeout = Timeout.InfiniteTimeSpan };
            _ownsHttpClient = true;
        }

        var transport = new QasimhaTransport(_http, _config);
        Checkout = new CheckoutResource(transport);
        Orders = new OrdersResource(transport);
        CheckoutLinks = new CheckoutLinksResource(transport);
        Settlements = new SettlementsResource(transport);
        Webhooks = new WebhooksResource(_config);
        WebhookEndpoints = new WebhookEndpointsResource(transport);
    }

    /// <summary>The SDK version, as carried in the <c>User-Agent</c> header.</summary>
    public static string Version => SdkVersion.Current;

    /// <summary>Checkout sessions: online, POS and QR.</summary>
    public CheckoutResource Checkout { get; }

    /// <summary>Orders: list, read by id or by your reference, capture, cancel, refund, fulfil, counts, contract.</summary>
    public OrdersResource Orders { get; }

    /// <summary>Payment links and QR orders.</summary>
    public CheckoutLinksResource CheckoutLinks { get; }

    /// <summary>Settlement batches and the settlement overview.</summary>
    public SettlementsResource Settlements { get; }

    /// <summary>Webhook signature verification and event parsing.</summary>
    public WebhooksResource Webhooks { get; }

    /// <summary>Your webhook endpoints: register, pause, rotate secrets, ping, delivery log (scope <c>webhooks:manage</c>).</summary>
    public WebhookEndpointsResource WebhookEndpoints { get; }

    /// <summary>The environment this client talks to.</summary>
    public QasimhaEnvironment Environment => _config.Environment;

    /// <summary>The base URL requests are sent to (without the <c>/api/v1</c> prefix).</summary>
    public Uri BaseUrl => new(_config.BaseUrl);

    /// <summary>The API key with its middle masked: <c>sk_live_…AbCd</c>. Safe to log.</summary>
    public string MaskedKey => _config.MaskedKey;

    /// <summary>The publishable key with its middle masked: <c>pk_live_…AbCd</c>, or <see langword="null"/> when none is configured.</summary>
    public string? MaskedPublishableKey => _config.MaskedPublishableKey;

    /// <inheritdoc />
    public override string ToString() =>
        $"QasimhaClient({(_config.Environment == QasimhaEnvironment.Production ? "production" : "sandbox")}, {_config.MaskedKey})";

    /// <summary>Disposes the HTTP resources this client owns. A caller-supplied <see cref="HttpClient"/> is left alone.</summary>
    public void Dispose()
    {
        if (_ownsHttpClient)
        {
            _http.Dispose();
        }

        GC.SuppressFinalize(this);
    }

    private static SocketsHttpHandler CreateDefaultHandler() => new()
    {
        AllowAutoRedirect = false,
        UseCookies = false,
        PooledConnectionLifetime = TimeSpan.FromMinutes(2),
    };
}
