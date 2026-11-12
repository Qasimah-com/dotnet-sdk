namespace Qasimha.Sdk.Http;

internal sealed class ApiRequest
{
    public ApiRequest(HttpMethod method, string path)
    {
        Method = method;
        Path = path;
    }

    public HttpMethod Method { get; }

    public string Path { get; }

    public IReadOnlyList<KeyValuePair<string, string>>? Query { get; init; }

    public object? Body { get; init; }

    public RequestOptions? Options { get; init; }

    public string Accept { get; init; } = "application/json";

    public string? PublicKey { get; init; }

    public string? Language { get; init; }
}
