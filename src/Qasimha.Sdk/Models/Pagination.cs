namespace Qasimha.Sdk;

/// <summary>Paging facts for a <see cref="Page{T}"/>.</summary>
public sealed class Pagination
{
    /// <summary>Rows across all pages.</summary>
    public long TotalItems { get; init; }

    /// <summary>The 1-based page number.</summary>
    public int Page { get; init; } = 1;

    /// <summary>Rows per page.</summary>
    public int PageSize { get; init; }

    /// <summary>Number of pages.</summary>
    public int TotalPages { get; init; } = 1;
}
