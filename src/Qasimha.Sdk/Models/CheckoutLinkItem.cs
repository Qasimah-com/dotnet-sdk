namespace Qasimha.Sdk;

/// <summary>One line of a checkout link's basket.</summary>
public sealed class CheckoutLinkItem
{
    /// <summary>Product name.</summary>
    public required string Name { get; set; }

    /// <summary>Stock-keeping unit.</summary>
    public string? Sku { get; set; }

    /// <summary>Units; at least 1. Default 1.</summary>
    public int Quantity { get; set; } = 1;

    /// <summary>Price per unit.</summary>
    public required decimal UnitPrice { get; set; }

    /// <summary>Product image URL.</summary>
    public string? ImageUrl { get; set; }
}
