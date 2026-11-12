namespace Qasimha.Sdk;

/// <summary>One basket line as returned on a checkout link.</summary>
public sealed class CheckoutLinkLine
{
    /// <summary>Product name.</summary>
    public string? Name { get; init; }

    /// <summary>Stock-keeping unit.</summary>
    public string? Sku { get; init; }

    /// <summary>Units.</summary>
    public int? Quantity { get; init; }

    /// <summary>Price per unit.</summary>
    public decimal? UnitPrice { get; init; }

    /// <summary>Line total.</summary>
    public decimal? Total { get; init; }

    /// <summary>Product image URL.</summary>
    public string? ImageUrl { get; init; }
}
