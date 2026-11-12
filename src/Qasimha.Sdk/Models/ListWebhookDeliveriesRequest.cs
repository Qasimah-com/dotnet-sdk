namespace Qasimha.Sdk;

/// <summary>Filters for <see cref="WebhookEndpointsResource.ListDeliveriesAsync"/>. Unset members are not sent.</summary>
public sealed class ListWebhookDeliveriesRequest
{
    /// <summary>A <see cref="WebhookDeliveryStatus"/> name. An unknown value is a 400 from the API.</summary>
    public string? Status { get; set; }

    /// <summary>1-based page. Default 1.</summary>
    public int? Page { get; set; }

    /// <summary>Rows per page. Default 20, at most 100.</summary>
    public int? PageSize { get; set; }
}
