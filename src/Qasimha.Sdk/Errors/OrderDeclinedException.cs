namespace Qasimha.Sdk;

/// <summary>
/// A 422 <c>ORDER_DECLINED</c>: the order was created but not approved, so there is nothing to pay.
/// <see cref="OrderId"/> identifies it; read it back with <see cref="OrdersResource.GetAsync"/> for the decision.
/// </summary>
public class OrderDeclinedException : QasimhaException
{
    /// <summary>Creates the error.</summary>
    /// <param name="message">What went wrong.</param>
    public OrderDeclinedException(string message)
        : base(message)
    {
    }

    /// <summary>Creates the error from an API response.</summary>
    /// <param name="message">The API's message.</param>
    /// <param name="details">The API-side facts.</param>
    /// <param name="orderId">The <c>data.installmentId</c> of the declined order.</param>
    /// <param name="innerException">The underlying exception, if any.</param>
    public OrderDeclinedException(string message, ApiErrorDetails details, string? orderId, Exception? innerException = null)
        : base(message, details, innerException)
    {
        OrderId = orderId;
    }

    /// <summary>The Qasimha id of the declined order, when the API returned it.</summary>
    public string? OrderId { get; }
}
