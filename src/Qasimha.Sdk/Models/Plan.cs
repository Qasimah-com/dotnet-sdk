using System.Text.Json.Serialization;
using Qasimha.Sdk.Json;

namespace Qasimha.Sdk;

/// <summary>One instalment plan quoted for an amount.</summary>
public sealed class Plan
{
    /// <summary>Number of instalments.</summary>
    public int Instalments { get; init; }

    /// <summary>How often an instalment falls due, for example <c>monthly</c>.</summary>
    [JsonConverter(typeof(LenientStringConverter))]
    public string? Frequency { get; init; }

    /// <summary>The regular instalment amount.</summary>
    public decimal InstalmentAmount { get; init; }

    /// <summary>The first instalment, paid at checkout.</summary>
    public decimal FirstInstalmentAmount { get; init; }

    /// <summary>The last instalment; it carries any rounding residual.</summary>
    public decimal LastInstalmentAmount { get; init; }

    /// <summary>Fees for the plan, VAT included.</summary>
    public decimal FeesIncludingVat { get; init; }

    /// <summary>The amount plus fees: what the customer pays in total.</summary>
    public decimal TotalPayable { get; init; }

    /// <summary>Whether the plan is Sharia-compliant.</summary>
    public bool ShariaCompliant { get; init; }
}
