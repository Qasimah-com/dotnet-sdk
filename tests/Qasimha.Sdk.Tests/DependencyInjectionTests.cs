using Microsoft.Extensions.DependencyInjection;
using Qasimha.Sdk.Extensions.DependencyInjection;
using Xunit;

namespace Qasimha.Sdk.Tests;

public class DependencyInjectionTests
{
    [Fact]
    public void Registers_a_client_built_from_the_configured_options()
    {
        var services = new ServiceCollection();
        services.AddQasimha(options =>
        {
            options.ApiKey = "sk_live_abc123";
            options.WebhookSecret = "whsec_x";
        });
        using var provider = services.BuildServiceProvider();

        var client = provider.GetRequiredService<QasimhaClient>();

        Assert.Equal(QasimhaEnvironment.Production, client.Environment);
        Assert.Equal("sk_live_…c123", client.MaskedKey);
        Assert.NotNull(client.Checkout);
        Assert.NotNull(client.Webhooks);
    }

    [Fact]
    public void The_configure_callback_can_use_the_service_provider()
    {
        var services = new ServiceCollection();
        services.AddSingleton(new KeyHolder("sk_test_fromdi"));
        services.AddQasimha((sp, options) => options.ApiKey = sp.GetRequiredService<KeyHolder>().Key);
        using var provider = services.BuildServiceProvider();

        var client = provider.GetRequiredService<QasimhaClient>();

        Assert.Equal(QasimhaEnvironment.Sandbox, client.Environment);
    }

    [Fact]
    public void Options_are_a_singleton_and_clients_are_transient()
    {
        var services = new ServiceCollection();
        services.AddQasimha(options => options.ApiKey = "sk_test_abc123");
        using var provider = services.BuildServiceProvider();

        Assert.Same(provider.GetRequiredService<QasimhaClientOptions>(), provider.GetRequiredService<QasimhaClientOptions>());
        Assert.NotSame(provider.GetRequiredService<QasimhaClient>(), provider.GetRequiredService<QasimhaClient>());
    }

    [Fact]
    public void Invalid_options_surface_at_resolution()
    {
        var services = new ServiceCollection();
        services.AddQasimha(options => options.ApiKey = "nope");
        using var provider = services.BuildServiceProvider();

        Assert.Throws<ConfigurationException>(() => provider.GetRequiredService<QasimhaClient>());
    }

    [Fact]
    public void Returns_the_http_client_builder_and_rejects_null_arguments()
    {
        var services = new ServiceCollection();

        var builder = services.AddQasimha(options => options.ApiKey = "sk_test_abc123");

        Assert.Equal(QasimhaServiceCollectionExtensions.HttpClientName, builder.Name);
        Assert.Throws<ArgumentNullException>(() => services.AddQasimha((Action<QasimhaClientOptions>)null!));
        Assert.Throws<ArgumentNullException>(() => services.AddQasimha((Action<IServiceProvider, QasimhaClientOptions>)null!));
    }

    private sealed record KeyHolder(string Key);
}
