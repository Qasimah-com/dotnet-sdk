using System.Net;
using System.Text;

namespace Qasimha.Sdk.Tests.Support;

internal sealed class FakeHandler : HttpMessageHandler
{
    private readonly Queue<Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>>> _responses = new();
    private Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>>? _last;

    public List<CapturedRequest> Requests { get; } = new();

    public FakeHandler Enqueue(int status, string? json = null, Action<HttpResponseMessage>? configure = null, string contentType = "application/json")
    {
        _responses.Enqueue((_, _) =>
        {
            var response = new HttpResponseMessage((HttpStatusCode)status);
            if (json is not null)
            {
                response.Content = new StringContent(json, Encoding.UTF8, contentType);
            }

            configure?.Invoke(response);
            return Task.FromResult(response);
        });
        return this;
    }

    public FakeHandler EnqueueBytes(int status, byte[] bytes, string contentType, string? fileName = null)
    {
        _responses.Enqueue((_, _) =>
        {
            var response = new HttpResponseMessage((HttpStatusCode)status) { Content = new ByteArrayContent(bytes) };
            response.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
            if (fileName is not null)
            {
                response.Content.Headers.ContentDisposition = new System.Net.Http.Headers.ContentDispositionHeaderValue("attachment") { FileName = fileName };
            }

            return Task.FromResult(response);
        });
        return this;
    }

    public FakeHandler EnqueueThrow(Exception exception)
    {
        _responses.Enqueue((_, _) => throw exception);
        return this;
    }

    public FakeHandler EnqueueHang()
    {
        _responses.Enqueue(async (_, cancellationToken) =>
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            throw new InvalidOperationException("unreachable");
        });
        return this;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var body = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var header in request.Headers.NonValidated)
        {
            headers[header.Key] = header.Value.ToString();
        }

        if (request.Content is not null)
        {
            foreach (var header in request.Content.Headers.NonValidated)
            {
                headers[header.Key] = header.Value.ToString();
            }
        }

        Requests.Add(new CapturedRequest(request.Method, request.RequestUri!, headers, body));

        if (_responses.Count > 0)
        {
            _last = _responses.Dequeue();
        }

        var next = _last ?? throw new InvalidOperationException("No response queued.");
        return await next(request, cancellationToken);
    }
}

internal sealed record CapturedRequest(HttpMethod Method, Uri Uri, Dictionary<string, string> Headers, string? Body)
{
    public string? Header(string name) => Headers.TryGetValue(name, out var value) ? value : null;
}
