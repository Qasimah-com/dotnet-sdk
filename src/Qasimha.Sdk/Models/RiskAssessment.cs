namespace Qasimha.Sdk;

/// <summary>Optional signals about the shopper's history with your store. Unset members are not sent.</summary>
public sealed class RiskAssessment
{
    /// <summary>Date of birth, <c>yyyy-MM-dd</c>.</summary>
    public string? CustomerDob { get; set; }

    /// <summary>Gender as your store records it.</summary>
    public string? CustomerGender { get; set; }

    /// <summary>Whether the shopper has bought from you before.</summary>
    public bool? IsExistingCustomer { get; set; }

    /// <summary>Whether the whole basket is digital.</summary>
    public bool? OrderIsDigital { get; set; }

    /// <summary>How many orders the shopper has placed with you.</summary>
    public int? TotalOrderCount { get; set; }

    /// <summary>Date of the shopper's first order with you, <c>yyyy-MM-dd</c>.</summary>
    public string? DateOfFirstTransaction { get; set; }
}
