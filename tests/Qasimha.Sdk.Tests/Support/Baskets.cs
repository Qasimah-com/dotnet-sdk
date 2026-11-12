namespace Qasimha.Sdk.Tests.Support;

internal static class Baskets
{
    public static CreateSessionRequest ThreeLine(string reference = "CART-000000-A") => new()
    {
        OrderReferenceId = reference,
        TotalAmount = 1302.10m,
        TaxAmount = 173.10m,
        ShippingAmount = 25.00m,
        DiscountAmount = 50.00m,
        Description = "Cart contract test",
        Items = new[]
        {
            Item("SKU-1", "Wireless Headphones", ItemType.Physical, 1, 899.00m),
            Item("SKU-2", "USB-C Cable", ItemType.Physical, 3, 45.00m),
            Item("SKU-3", "Extended Warranty", ItemType.Digital, 1, 120.00m),
        },
        Consumer = Noura(),
        ShippingAddress = Riyadh(),
    };

    public static CreateSessionRequest Single(string reference, decimal total, CheckoutItem item, decimal tax = 0m, decimal shipping = 0m, decimal discount = 0m) => new()
    {
        OrderReferenceId = reference,
        TotalAmount = total,
        TaxAmount = tax,
        ShippingAmount = shipping,
        DiscountAmount = discount,
        Description = "Cart contract test",
        Items = new[] { item },
        Consumer = Noura(),
        ShippingAddress = Riyadh(),
    };

    public static CheckoutItem Item(string reference, string name, string type, int quantity, decimal unitPrice) => new()
    {
        ReferenceId = reference,
        Name = name,
        Type = type,
        Sku = reference,
        Quantity = quantity,
        UnitPrice = unitPrice,
        TotalAmount = quantity * unitPrice,
    };

    public static Consumer Noura() => new()
    {
        FirstName = "Noura",
        LastName = "Al-Shammari",
        PhoneNumber = "+966512349999",
        Email = "shopper@example.com",
    };

    public static Address Riyadh() => new()
    {
        FirstName = "Noura",
        LastName = "Al-Shammari",
        Line1 = "King Fahd Rd",
        City = "Riyadh",
        Region = "Riyadh",
        PostalCode = "12345",
        CountryCode = "SA",
        PhoneNumber = "+966512349999",
    };

    public static CreatePosSessionRequest Pos(string reference = "POS-000000-A") => new()
    {
        OrderReferenceId = reference,
        TotalAmount = 1154.00m,
        Items = ThreeLine().Items,
        Consumer = Noura(),
    };

    public static CreateQrSessionRequest Qr(string reference = "QR-000000-A") => new()
    {
        OrderReferenceId = reference,
        TotalAmount = 1154.00m,
        Items = ThreeLine().Items,
    };
}
