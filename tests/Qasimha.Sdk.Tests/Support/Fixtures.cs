using System.Text.Json;

namespace Qasimha.Sdk.Tests.Support;

internal static class Fixtures
{
    public static string Read(string name) =>
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "fixtures", name));

    public static JsonDocument Json(string name) => JsonDocument.Parse(Read(name));

    public static string Envelope(string code, object? data, bool success = true, string? message = null, object? errors = null)
    {
        var envelope = new Dictionary<string, object?>
        {
            ["success"] = success,
            ["uuid"] = "4d51526b-617e-4c37-a3f3-20f209d87b69",
            ["code"] = code,
            ["message"] = message ?? code,
            ["data"] = data,
            ["errors"] = errors,
        };
        return JsonSerializer.Serialize(envelope);
    }

    public const string SessionCreated = """
        {"success":true,"uuid":"11111111-1111-4111-8111-111111111111","code":"SESSION_CREATED","message":"Session created",
         "data":{"installmentId":"9a2e6c4b-1111-4c2d-8a1f-000000000001","providerSessionId":"sess_1","paymentUrl":"https://localhost/checkout/abc","status":"New"},
         "errors":null}
        """;

    public const string Duplicate = """
        {"success":false,"uuid":"22222222-2222-4222-8222-222222222222","code":"DUPLICATE_ORDER_REFERENCE","message":"Order reference already in use",
         "data":{"installmentId":"9a2e6c4b-1111-4c2d-8a1f-000000000001","paymentUrl":"https://localhost/checkout/abc","status":"AwaitingCheckout"},
         "errors":null}
        """;

    public const string DuplicateWithoutOrder = """
        {"success":false,"uuid":"22222222-2222-4222-8222-222222222223","code":"DUPLICATE_ORDER_REFERENCE","message":"Order reference already in use","data":null,"errors":null}
        """;

    public const string Declined = """
        {"success":false,"uuid":"33333333-3333-4333-8333-333333333333","code":"ORDER_DECLINED","message":"The order was declined",
         "data":{"installmentId":"76c94cf2-db05-4c5a-b8da-bb05d1ae7cbc","providerSessionId":null,"paymentUrl":null,"status":"Declined"},
         "errors":null}
        """;

    public const string ValidationError = """
        {"success":false,"uuid":"44444444-4444-4444-8444-444444444444","code":"VALIDATION_ERROR","message":"Validation failed","data":null,
         "errors":[{"field":"TotalAmount","message":"Must be greater than 0"},{"field":"Items[0].Quantity","message":"Must be at least 1"}]}
        """;
}
