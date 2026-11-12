namespace Qasimha.Sdk;

/// <summary>An unpaged list result.</summary>
/// <typeparam name="T">The row type.</typeparam>
public class ApiList<T> : ApiResource
{
    /// <summary>The rows.</summary>
    public IReadOnlyList<T> Items { get; internal set; } = Array.Empty<T>();
}
