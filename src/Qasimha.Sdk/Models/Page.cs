namespace Qasimha.Sdk;

/// <summary>A paged list result.</summary>
/// <typeparam name="T">The row type.</typeparam>
public sealed class Page<T> : ApiList<T>
{
    /// <summary>Where this page sits in the whole.</summary>
    public Pagination Pagination { get; internal set; } = new();
}
