namespace Qasimha.Sdk;

/// <summary>A shipping or billing address.</summary>
public sealed class Address
{
    /// <summary>Recipient given name.</summary>
    public required string FirstName { get; set; }

    /// <summary>Recipient family name.</summary>
    public required string LastName { get; set; }

    /// <summary>Street line.</summary>
    public required string Line1 { get; set; }

    /// <summary>Second street line.</summary>
    public string? Line2 { get; set; }

    /// <summary>City.</summary>
    public required string City { get; set; }

    /// <summary>Region or province.</summary>
    public string? Region { get; set; }

    /// <summary>Postal code.</summary>
    public string? PostalCode { get; set; }

    /// <summary>ISO 3166-1 alpha-2 country code. Default <c>SA</c>.</summary>
    public string CountryCode { get; set; } = "SA";

    /// <summary>Contact number for the delivery.</summary>
    public string? PhoneNumber { get; set; }
}
