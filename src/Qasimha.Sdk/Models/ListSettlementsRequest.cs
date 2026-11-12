namespace Qasimha.Sdk;

/// <summary>Paging for <see cref="SettlementsResource.ListAsync"/>. Unset members are not sent.</summary>
public sealed class ListSettlementsRequest
{
    /// <summary>1-based page. Default 1.</summary>
    public int? Page { get; set; }

    /// <summary>Rows per page. Default 20.</summary>
    public int? PageSize { get; set; }
}
