using System.Text.Json;
using Qasimha.Sdk.Json;

namespace Qasimha.Sdk.Http;

internal static class Materializer
{
    public static T Model<T>(ApiResponse response)
        where T : ApiResource, new()
    {
        if (response.Envelope is null)
        {
            throw Unexpected(response, "The response body is not an API envelope.", null);
        }

        try
        {
            return FromElement<T>(response.Envelope.Data, response.Info);
        }
        catch (JsonException ex)
        {
            throw Unexpected(response, "The response payload could not be decoded: " + ex.Message, ex);
        }
    }

    public static T FromElement<T>(JsonElement data, ResponseInfo? info)
        where T : ApiResource, new()
    {
        var model = data.ValueKind == JsonValueKind.Object
            ? JsonSerializer.Deserialize<T>(data, JsonDefaults.Options) ?? new T()
            : new T();
        model.Raw = data;
        model.Response = info;
        model.AfterMaterialize();
        return model;
    }

    public static Page<T> PageOf<T>(ApiResponse response, int? requestedPage, int? requestedPageSize)
    {
        if (response.Envelope is null)
        {
            throw Unexpected(response, "The response body is not an API envelope.", null);
        }

        try
        {
            var data = response.Envelope.Data;
            var items = Rows<T>(data);
            Pagination? pagination = null;
            if (data.TryGetPropertyIgnoreCase("pagination", out var element) && element.ValueKind == JsonValueKind.Object)
            {
                pagination = JsonSerializer.Deserialize<Pagination>(element, JsonDefaults.Options);
            }

            pagination ??= new Pagination
            {
                TotalItems = items.Count,
                Page = requestedPage ?? 1,
                PageSize = requestedPageSize ?? items.Count,
                TotalPages = 1,
            };

            return new Page<T> { Items = items, Pagination = pagination, Raw = data, Response = response.Info };
        }
        catch (JsonException ex)
        {
            throw Unexpected(response, "The response payload could not be decoded: " + ex.Message, ex);
        }
    }

    public static ApiList<T> ListOf<T>(ApiResponse response)
    {
        if (response.Envelope is null)
        {
            throw Unexpected(response, "The response body is not an API envelope.", null);
        }

        try
        {
            var data = response.Envelope.Data;
            return new ApiList<T> { Items = Rows<T>(data), Raw = data, Response = response.Info };
        }
        catch (JsonException ex)
        {
            throw Unexpected(response, "The response payload could not be decoded: " + ex.Message, ex);
        }
    }

    private static IReadOnlyList<T> Rows<T>(JsonElement data)
    {
        var rows = data;
        if (data.ValueKind == JsonValueKind.Object
            && !data.TryGetPropertyIgnoreCase("data", out rows)
            && !data.TryGetPropertyIgnoreCase("items", out rows))
        {
            return Array.Empty<T>();
        }

        if (rows.ValueKind != JsonValueKind.Array)
        {
            return Array.Empty<T>();
        }

        return JsonSerializer.Deserialize<List<T>>(rows, JsonDefaults.Options) ?? new List<T>();
    }

    private static ApiException Unexpected(ApiResponse response, string message, Exception? inner) =>
        new(message, new ApiErrorDetails(response.Envelope?.Code, response.Status, response.Info.RequestId, null, response.Envelope), inner)
        {
            Response = response.Info,
        };
}
