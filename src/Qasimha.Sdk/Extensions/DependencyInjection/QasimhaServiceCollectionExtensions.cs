using Microsoft.Extensions.DependencyInjection;

namespace Qasimha.Sdk.Extensions.DependencyInjection;

/// <summary>Registers <see cref="QasimhaClient"/> on <see cref="IHttpClientFactory"/>.</summary>
public static class QasimhaServiceCollectionExtensions
{
    /// <summary>The named <see cref="HttpClient"/> the SDK is registered on.</summary>
    public const string HttpClientName = "Qasimha.Sdk";

    /// <summary>
    /// Registers <see cref="QasimhaClientOptions"/> as a singleton and <see cref="QasimhaClient"/> as a
    /// typed client over a primary handler with redirects disabled. Returns the builder so you can add
    /// handlers or resilience of your own.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Fills the options.</param>
    public static IHttpClientBuilder AddQasimha(this IServiceCollection services, Action<QasimhaClientOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        return services.AddQasimha((_, options) => configure(options));
    }

    /// <summary>
    /// Registers <see cref="QasimhaClientOptions"/> as a singleton and <see cref="QasimhaClient"/> as a
    /// typed client over a primary handler with redirects disabled. The options are filled with access
    /// to the service provider.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Fills the options.</param>
    public static IHttpClientBuilder AddQasimha(this IServiceCollection services, Action<IServiceProvider, QasimhaClientOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        services.AddSingleton(provider =>
        {
            var options = new QasimhaClientOptions();
            configure(provider, options);
            return options;
        });

        return services
            .AddHttpClient<QasimhaClient, QasimhaClient>(
                HttpClientName,
                (http, provider) => new QasimhaClient(provider.GetRequiredService<QasimhaClientOptions>(), http))
            .ConfigureHttpClient(http => http.Timeout = Timeout.InfiniteTimeSpan)
            .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
            {
                AllowAutoRedirect = false,
                UseCookies = false,
            });
    }
}
