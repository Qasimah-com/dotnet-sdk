namespace Qasimha.Sdk;

/// <summary>The shopper. The SDK never logs any of these fields.</summary>
public sealed class Consumer
{
    /// <summary>Given name.</summary>
    public required string FirstName { get; set; }

    /// <summary>Family name.</summary>
    public required string LastName { get; set; }

    /// <summary>Mobile number in international format, for example <c>+9665XXXXXXXX</c>.</summary>
    public required string PhoneNumber { get; set; }

    /// <summary>Email address. Required for <see cref="CreatePosSessionRequest"/>.</summary>
    public string? Email { get; set; }

    /// <summary>National ID or Iqama: 10 digits starting with 1 or 2, when given.</summary>
    public string? NationalId { get; set; }
}
