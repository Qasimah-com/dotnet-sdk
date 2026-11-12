using Qasimha.Sdk.Json;

namespace Qasimha.Sdk.Http;

internal static class ReplayedCreate
{
    public static bool Matches(DuplicateOrderReferenceException exception) =>
        exception.Response is { Attempt: > 1 }
        && exception.RawEnvelope is not null
        && (exception.ExistingOrderId is not null || exception.RawEnvelope.Data.GetStringOrNull("id") is not null);

    public static void Log(QasimhaTransport transport, DuplicateOrderReferenceException exception, string path)
    {
        var info = exception.Response;
        transport.Log(
            QasimhaLogLevel.Info,
            "duplicate order reference on a retry treated as the earlier create",
            new Dictionary<string, object?>
            {
                ["method"] = "POST",
                ["path"] = "/api/v1" + path,
                ["status"] = info?.HttpStatus,
                ["durationMs"] = null,
                ["requestId"] = info?.RequestId,
                ["attempt"] = info?.Attempt,
                ["code"] = exception.Code,
            });
    }
}
