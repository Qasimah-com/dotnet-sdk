using System.Text.Json.Serialization;
using Qasimha.Sdk.Json;

namespace Qasimha.Sdk;

/// <summary>One instalment of an order's schedule.</summary>
public sealed class OrderInstallment
{
    /// <summary>1-based position in the schedule.</summary>
    public int? Sequence { get; init; }

    /// <summary>When it is due.</summary>
    public DateTimeOffset? DueDate { get; init; }

    /// <summary>The amount due.</summary>
    public decimal? Amount { get; init; }

    /// <summary>The principal part of the amount.</summary>
    public decimal? Principal { get; init; }

    /// <summary>The profit part of the amount.</summary>
    public decimal? Profit { get; init; }

    /// <summary>The instalment status, as an <see cref="InstallmentItemStatus"/> name.</summary>
    [JsonConverter(typeof(InstallmentItemStatusConverter))]
    public string? Status { get; init; }

    /// <summary>The amount paid so far.</summary>
    public decimal? PaidAmount { get; init; }

    /// <summary>When it was paid.</summary>
    public DateTimeOffset? PaidAt { get; init; }

    /// <summary>The payment provider's reference for the payment.</summary>
    public string? ProviderRef { get; init; }
}
