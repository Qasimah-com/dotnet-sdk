using System.Text.Json.Serialization;

namespace Qasimha.Sdk;

/// <summary>
/// Result of <see cref="CheckoutResource.GetPlansAsync"/>: every plan the merchant's policy offers for
/// the amount, quoted from the policy's price list. The checkout quotes the customer's own terms.
/// </summary>
public sealed class Plans : ApiResource
{
    private IReadOnlyList<Plan> _items = Array.Empty<Plan>();

    /// <summary>The merchant's display name.</summary>
    public string? MerchantName { get; init; }

    /// <summary>ISO 4217 currency code of every amount here (<c>SAR</c>).</summary>
    public string? Currency { get; init; }

    /// <summary>The smallest amount the policy finances.</summary>
    public decimal MinAmount { get; init; }

    /// <summary>The largest amount the policy finances.</summary>
    public decimal MaxAmount { get; init; }

    /// <summary>The plans on offer; empty when the amount is outside <c>[MinAmount, MaxAmount]</c> or no policy is active.</summary>
    [JsonPropertyName("plans")]
    public IReadOnlyList<Plan> Items
    {
        get => _items;
        init => _items = value ?? Array.Empty<Plan>();
    }
}
