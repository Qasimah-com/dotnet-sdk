using System.Text.Json.Serialization;
using Qasimha.Sdk.Json;

namespace Qasimha.Sdk;

/// <summary>One settlement batch paid to you, net of fees.</summary>
public sealed class Settlement
{
    /// <summary>The settlement id.</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>The payment provider's settlement id.</summary>
    public string? ProviderSettlementId { get; init; }

    /// <summary>The amount settled.</summary>
    public decimal? Amount { get; init; }

    /// <summary>ISO 4217 currency code.</summary>
    public string? CurrencyCode { get; init; }

    /// <summary>When it was settled.</summary>
    public DateTimeOffset? SettledAt { get; init; }

    /// <summary>The settlement status.</summary>
    [JsonConverter(typeof(LenientStringConverter))]
    public string? Status { get; init; }

    /// <summary>How many orders the batch covers.</summary>
    public int? OrderCount { get; init; }
}
