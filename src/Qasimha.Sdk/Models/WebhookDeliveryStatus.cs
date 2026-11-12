namespace Qasimha.Sdk;

/// <summary>Delivery states, as filtered by <see cref="ListWebhookDeliveriesRequest.Status"/> and reported by <see cref="WebhookDelivery.Status"/>.</summary>
public static class WebhookDeliveryStatus
{
    /// <summary>Queued, or waiting for a retry.</summary>
    public const string Pending = "Pending";

    /// <summary>Your server answered 2xx.</summary>
    public const string Succeeded = "Succeeded";

    /// <summary>Every attempt failed, or the endpoint was deleted while retries were pending.</summary>
    public const string Failed = "Failed";
}
