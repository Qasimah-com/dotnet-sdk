using System.Diagnostics;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using Qasimha.Sdk.Json;

namespace Qasimha.Sdk.Http;

internal sealed class QasimhaTransport
{
    private const string ApiPrefix = "/api/v1";

    private readonly HttpClient _http;

    public QasimhaTransport(HttpClient http, ClientConfiguration configuration)
    {
        _http = http;
        Configuration = configuration;
    }

    public ClientConfiguration Configuration { get; }

    public async Task<ApiResponse> SendAsync(ApiRequest request, CancellationToken cancellationToken)
    {
        var correlationId = Guid.NewGuid().ToString();
        var idempotencyKey = ResolveIdempotencyKey(request);
        var retriesAllowed = request.Method == HttpMethod.Get || idempotencyKey is not null;
        var body = request.Body is null
            ? null
            : JsonSerializer.SerializeToUtf8Bytes(request.Body, request.Body.GetType(), JsonDefaults.Options);
        var uri = BuildUri(request);

        for (var attempt = 1; ; attempt++)
        {
            var stopwatch = Stopwatch.StartNew();
            ApiResponse? response = null;
            Exception? transportFailure = null;

            try
            {
                response = await SendOnceAsync(request, uri, body, idempotencyKey, correlationId, attempt, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (HttpRequestException ex)
            {
                transportFailure = ex;
            }
            catch (IOException ex)
            {
                transportFailure = ex;
            }
            catch (SocketException ex)
            {
                transportFailure = ex;
            }
            catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
            {
                transportFailure = ex;
            }

            var canRetry = retriesAllowed && attempt <= Configuration.MaxRetries;

            if (response is null)
            {
                var failure = new ConnectionException(DescribeTransportFailure(transportFailure), transportFailure);
                Configuration.Log(
                    canRetry ? QasimhaLogLevel.Warn : QasimhaLogLevel.Error,
                    canRetry ? "request failed, retrying" : "request failed",
                    Fields(request, null, stopwatch, null, attempt, null));
                if (!canRetry)
                {
                    throw failure;
                }

                await Configuration.Delay.DelayAsync(RetryPolicy.Backoff(attempt), cancellationToken).ConfigureAwait(false);
                continue;
            }

            var fields = Fields(request, response.Status, stopwatch, response.Info.RequestId, attempt, response.Envelope?.Code);
            if (response.IsSuccess)
            {
                Configuration.Log(QasimhaLogLevel.Debug, "request completed", fields);
                return response;
            }

            var error = ErrorMapper.Map(response);
            var retry = canRetry && RetryPolicy.IsRetryable(response);
            Configuration.Log(
                retry ? QasimhaLogLevel.Warn : QasimhaLogLevel.Error,
                retry ? "request failed, retrying" : "request failed",
                fields);
            if (!retry)
            {
                throw error;
            }

            var wait = response.Status == 429 && response.RetryAfter is { } retryAfter
                ? retryAfter
                : RetryPolicy.Backoff(attempt);
            await Configuration.Delay.DelayAsync(wait, cancellationToken).ConfigureAwait(false);
        }
    }

    public void Log(QasimhaLogLevel level, string message, IReadOnlyDictionary<string, object?> fields) =>
        Configuration.Log(level, message, fields);

    public static string PathOf(ApiRequest request) => ApiPrefix + request.Path;

    private async Task<ApiResponse> SendOnceAsync(
        ApiRequest request,
        Uri uri,
        byte[]? body,
        string? idempotencyKey,
        string correlationId,
        int attempt,
        CancellationToken cancellationToken)
    {
        using var message = new HttpRequestMessage(request.Method, uri);
        if (request.PublicKey is not null)
        {
            message.Headers.TryAddWithoutValidation("X-Public-Key", request.PublicKey);
        }
        else
        {
            message.Headers.TryAddWithoutValidation("X-Api-Key", Configuration.ApiKey);
        }

        message.Headers.TryAddWithoutValidation("Accept", request.Accept);
        message.Headers.TryAddWithoutValidation("Accept-Language", request.Language ?? Configuration.Language);
        message.Headers.TryAddWithoutValidation("User-Agent", Configuration.UserAgent);
        message.Headers.TryAddWithoutValidation("X-Correlation-Id", correlationId);
        if (idempotencyKey is not null)
        {
            message.Headers.TryAddWithoutValidation("Idempotency-Key", idempotencyKey);
        }

        if (body is not null)
        {
            message.Content = new ByteArrayContent(body);
            message.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json") { CharSet = "utf-8" };
        }

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(Configuration.Timeout);

        using var response = await _http.SendAsync(message, HttpCompletionOption.ResponseContentRead, timeout.Token)
            .ConfigureAwait(false);
        var bytes = await response.Content.ReadAsByteArrayAsync(timeout.Token).ConfigureAwait(false);
        return ApiResponse.From(response, bytes, correlationId, attempt, Configuration.Clock());
    }

    private static string? ResolveIdempotencyKey(ApiRequest request)
    {
        if (request.Method != HttpMethod.Post)
        {
            return null;
        }

        var options = request.Options;
        if (options is { Idempotent: false })
        {
            return null;
        }

        var provided = options?.IdempotencyKey;
        return string.IsNullOrWhiteSpace(provided) ? Guid.NewGuid().ToString() : provided.Trim();
    }

    private Uri BuildUri(ApiRequest request)
    {
        var builder = new StringBuilder(Configuration.BaseUrl).Append(ApiPrefix).Append(request.Path);
        if (request.Query is { Count: > 0 })
        {
            var separator = '?';
            foreach (var (key, value) in request.Query)
            {
                builder.Append(separator).Append(Uri.EscapeDataString(key)).Append('=').Append(Uri.EscapeDataString(value));
                separator = '&';
            }
        }

        return new Uri(builder.ToString(), UriKind.Absolute);
    }

    private string DescribeTransportFailure(Exception? exception) => exception switch
    {
        OperationCanceledException => FormattableString.Invariant(
            $"The request timed out after {Configuration.Timeout.TotalSeconds:0.###} s."),
        null => "The request failed before a response was received.",
        _ => "Could not reach the API: " + exception.Message,
    };

    private static Dictionary<string, object?> Fields(
        ApiRequest request,
        int? status,
        Stopwatch stopwatch,
        string? requestId,
        int attempt,
        string? code) => new()
        {
            ["method"] = request.Method.Method,
            ["path"] = ApiPrefix + request.Path,
            ["status"] = status,
            ["durationMs"] = stopwatch.ElapsedMilliseconds,
            ["requestId"] = requestId,
            ["attempt"] = attempt,
            ["code"] = code,
        };
}
