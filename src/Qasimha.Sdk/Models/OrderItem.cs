namespace Qasimha.Sdk;

/// <summary>One basket line as stored on the order.</summary>
public sealed class OrderItem
{
    /// <summary>Your line reference.</summary>
    public string? ReferenceId { get; init; }

    /// <summary>Product name.</summary>
    public string? Name { get; init; }

    /// <summary><see cref="ItemType.Physical"/> or <see cref="ItemType.Digital"/>.</summary>
    public string? Type { get; init; }

    /// <summary>Stock-keeping unit.</summary>
    public string? Sku { get; init; }

    /// <summary>Units.</summary>
    public int Quantity { get; init; }

    /// <summary>Price per unit.</summary>
    public decimal UnitPrice { get; init; }

    /// <summary>Line total.</summary>
    public decimal TotalAmount { get; init; }

    /// <summary>Line tax.</summary>
    public decimal TaxAmount { get; init; }

    /// <summary>Line discount.</summary>
    public decimal DiscountAmount { get; init; }

    /// <summary>Product image URL.</summary>
    public string? ImageUrl { get; init; }
}
