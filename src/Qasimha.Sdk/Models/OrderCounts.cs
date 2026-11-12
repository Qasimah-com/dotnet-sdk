namespace Qasimha.Sdk;

/// <summary>Result of <see cref="OrdersResource.CountsAsync"/>: order counts per portal tab.</summary>
public sealed class OrderCounts : ApiResource
{
    /// <summary>All orders.</summary>
    public long All { get; init; }

    /// <summary>Approved orders.</summary>
    public long Approved { get; init; }

    /// <summary>Authorised orders.</summary>
    public long Authorised { get; init; }

    /// <summary>Captured orders.</summary>
    public long Captured { get; init; }

    /// <summary>Refunded orders.</summary>
    public long Refunded { get; init; }

    /// <summary>Cancelled orders.</summary>
    public long Cancelled { get; init; }
}
