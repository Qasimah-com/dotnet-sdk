namespace Qasimha.Sdk;

/// <summary>
/// A 409 <c>DUPLICATE_ORDER_REFERENCE</c>: the <c>orderReferenceId</c> is already in use. When the
/// existing order is yours, <see cref="ExistingOrderId"/> and <see cref="ExistingPaymentUrl"/> point
/// at the checkout already open, so send the shopper there rather than creating a second order.
/// </summary>
public class DuplicateOrderReferenceException : QasimhaException
{
    /// <summary>Creates the error.</summary>
    /// <param name="message">What went wrong.</param>
    public DuplicateOrderReferenceException(string message)
        : base(message)
    {
    }

    /// <summary>Creates the error from an API response.</summary>
    /// <param name="message">The API's message.</param>
    /// <param name="details">The API-side facts.</param>
    /// <param name="existingOrderId">The <c>data.installmentId</c> of the order already open.</param>
    /// <param name="existingPaymentUrl">The <c>data.paymentUrl</c> of that order, when present.</param>
    /// <param name="innerException">The underlying exception, if any.</param>
    public DuplicateOrderReferenceException(
        string message,
        ApiErrorDetails details,
        string? existingOrderId,
        string? existingPaymentUrl,
        Exception? innerException = null)
        : base(message, details, innerException)
    {
        ExistingOrderId = existingOrderId;
        ExistingPaymentUrl = existingPaymentUrl;
    }

    /// <summary>The Qasimha id of the order that already carries this reference, when the API returned it.</summary>
    public string? ExistingOrderId { get; }

    /// <summary>The checkout URL of that order, when the API returned it.</summary>
    public string? ExistingPaymentUrl { get; }
}
