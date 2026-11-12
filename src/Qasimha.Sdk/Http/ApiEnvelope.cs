using System.Text.Json;
using Qasimha.Sdk.Json;

namespace Qasimha.Sdk;

/// <summary>The decoded response envelope: <c>{ success, uuid, code, message, data, errors }</c>.</summary>
public sealed class ApiEnvelope
{
    /// <summary>Creates an envelope.</summary>
    /// <param name="success">The <c>success</c> flag.</param>
    /// <param name="uuid">The request correlation id.</param>
    /// <param name="code">The machine-readable code.</param>
    /// <param name="message">The human-readable message.</param>
    /// <param name="data">The payload, or an undefined element when absent.</param>
    /// <param name="errors">Field-level errors.</param>
    public ApiEnvelope(bool success, string? uuid, string? code, string? message, JsonElement data, IReadOnlyList<ApiFieldError>? errors)
    {
        Success = success;
        Uuid = uuid;
        Code = code;
        Message = message;
        Data = data;
        Errors = errors ?? Array.Empty<ApiFieldError>();
    }

    /// <summary>The <c>success</c> flag.</summary>
    public bool Success { get; }

    /// <summary>The request correlation id.</summary>
    public string? Uuid { get; }

    /// <summary>The machine-readable code, for example <c>SESSION_CREATED</c> or <c>VALIDATION_ERROR</c>.</summary>
    public string? Code { get; }

    /// <summary>The human-readable message, localised by <c>Accept-Language</c>.</summary>
    public string? Message { get; }

    /// <summary>The payload as decoded JSON; <see cref="JsonValueKind.Undefined"/> when the envelope had none.</summary>
    public JsonElement Data { get; }

    /// <summary>Field-level errors, with field names in the API's PascalCase.</summary>
    public IReadOnlyList<ApiFieldError> Errors { get; }

    internal bool HasSuccessFlag { get; init; }

    internal static ApiEnvelope? TryParse(byte[] body)
    {
        try
        {
            using var document = JsonDocument.Parse(body);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            bool? success = null;
            string? uuid = null;
            string? code = null;
            string? message = null;
            JsonElement data = default;
            List<ApiFieldError>? errors = null;

            foreach (var property in root.EnumerateObject())
            {
                if (Is(property, "success") || Is(property, "isSuccess"))
                {
                    if (property.Value.ValueKind is JsonValueKind.True or JsonValueKind.False)
                    {
                        success = property.Value.GetBoolean();
                    }
                }
                else if (Is(property, "uuid"))
                {
                    uuid = property.Value.AsStringOrNull();
                }
                else if (Is(property, "code"))
                {
                    code = property.Value.AsStringOrNull();
                }
                else if (Is(property, "message"))
                {
                    message = property.Value.AsStringOrNull();
                }
                else if (Is(property, "data"))
                {
                    data = property.Value.Clone();
                }
                else if (Is(property, "errors") && property.Value.ValueKind == JsonValueKind.Array)
                {
                    errors = ParseErrors(property.Value);
                }
            }

            return new ApiEnvelope(success ?? false, uuid, code, message, data, errors) { HasSuccessFlag = success.HasValue };
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static bool Is(JsonProperty property, string name) =>
        string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase);

    private static List<ApiFieldError> ParseErrors(JsonElement array)
    {
        var errors = new List<ApiFieldError>();
        foreach (var item in array.EnumerateArray())
        {
            if (item.ValueKind == JsonValueKind.Object)
            {
                errors.Add(new ApiFieldError(item.GetStringOrNull("field"), item.GetStringOrNull("message")));
            }
            else if (item.ValueKind == JsonValueKind.String)
            {
                errors.Add(new ApiFieldError(null, item.GetString()));
            }
        }

        return errors;
    }
}
