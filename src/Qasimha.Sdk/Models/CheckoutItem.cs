namespace Qasimha.Sdk;

/// <summary>One basket line. Every field is stored as sent and read back on the order.</summary>
public sealed class CheckoutItem
{
    /// <summary>Your line reference (often the SKU).</summary>
    public required string ReferenceId { get; set; }

    /// <summary>Product name, shown to the shopper.</summary>
    public required string Name { get; set; }

    /// <summary><see cref="ItemType.Physical"/> or <see cref="ItemType.Digital"/>.</summary>
    public required string Type { get; set; }

    /// <summary>Stock-keeping unit.</summary>
    public string? Sku { get; set; }

    /// <summary>Units; at least 1.</summary>
    public required int Quantity { get; set; }

    /// <summary>Price per unit.</summary>
    public required decimal UnitPrice { get; set; }

    /// <summary>Line total: <c>Quantity × UnitPrice − DiscountAmount</c>.</summary>
    public required decimal TotalAmount { get; set; }

    /// <summary>Line tax. Default 0.</summary>
    public decimal TaxAmount { get; set; }

    /// <summary>Line discount. Default 0.</summary>
    public decimal DiscountAmount { get; set; }

    /// <summary>Product image shown on the hosted checkout.</summary>
    public string? ImageUrl { get; set; }
}
