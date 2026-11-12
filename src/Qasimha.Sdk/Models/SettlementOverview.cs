using System.Text.Json;
using System.Text.Json.Serialization;
using Qasimha.Sdk.Json;

namespace Qasimha.Sdk;

/// <summary>Result of <see cref="SettlementsResource.OverviewAsync"/>. The API's headline figures are typed; the rest is in <see cref="ApiResource.Raw"/>.</summary>
public sealed class SettlementOverview : ApiResource
{
    /// <summary>Total settled to you so far.</summary>
    public decimal? TotalSettled { get; init; }

    /// <summary>Number of settlement batches.</summary>
    public long? Count { get; init; }

    /// <summary>The most recent settlement, as returned.</summary>
    public JsonElement? LastSettlement { get; init; }

    /// <summary>The settlement cycle.</summary>
    [JsonConverter(typeof(LenientStringConverter))]
    public string? SettlementCycle { get; init; }

    /// <summary>The settlement model.</summary>
    [JsonConverter(typeof(LenientStringConverter))]
    public string? SettlementModel { get; init; }

    /// <summary>The minimum batch amount before a settlement is paid.</summary>
    public decimal? MinimumSettlementAmount { get; init; }

    /// <summary>What is owed to you but not yet settled.</summary>
    public decimal? RemainingForNextSettlement { get; init; }
}
