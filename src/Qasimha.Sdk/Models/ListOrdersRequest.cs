namespace Qasimha.Sdk;

/// <summary>Filters for <see cref="OrdersResource.ListAsync"/>. Unset members are not sent.</summary>
public sealed class ListOrdersRequest
{
    /// <summary>An <see cref="OrderStatus"/> name or an <see cref="OrderStatusFilter"/> alias. An unknown value answers <c>400 INVALID_STATUS</c>.</summary>
    public string? Status { get; set; }

    /// <summary>Matches your order reference, the customer's name, email or phone. For an exact lookup by reference use <see cref="OrdersResource.GetByReferenceAsync"/>.</summary>
    public string? Search { get; set; }

    /// <summary>1-based page. Default 1.</summary>
    public int? Page { get; set; }

    /// <summary>Rows per page. Default 20.</summary>
    public int? PageSize { get; set; }
}
