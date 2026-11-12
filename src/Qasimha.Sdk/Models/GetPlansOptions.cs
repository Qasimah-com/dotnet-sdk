namespace Qasimha.Sdk;

/// <summary>Per-call options for <see cref="CheckoutResource.GetPlansAsync"/>.</summary>
public sealed class GetPlansOptions
{
    /// <summary>Language of the quote, <c>"en"</c> or <c>"ar"</c>, sent as <c>lang</c> and <c>Accept-Language</c>. Defaults to the client's <see cref="QasimhaClientOptions.Language"/>.</summary>
    public string? Lang { get; set; }

    /// <summary>The publishable key for this call. Defaults to <see cref="QasimhaClientOptions.PublishableKey"/>; must be from the same environment as the API key.</summary>
    public string? PublishableKey { get; set; }
}
