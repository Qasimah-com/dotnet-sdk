namespace Qasimha.Sdk;

/// <summary>The customer on an order.</summary>
public sealed class OrderCustomer
{
    /// <summary>The Qasimha customer id.</summary>
    public string? Id { get; init; }

    /// <summary>Full name.</summary>
    public string? Name { get; init; }

    /// <summary>Email address.</summary>
    public string? Email { get; init; }

    /// <summary>Phone number.</summary>
    public string? Phone { get; init; }

    /// <summary>National ID or Iqama, when known.</summary>
    public string? NationalId { get; init; }
}
