using System.Text.Json;
using System.Text.Json.Serialization;

namespace Qasimha.Sdk;

/// <summary>Base of every top-level result: the decoded envelope <c>data</c> and the response it came from.</summary>
public abstract class ApiResource
{
    /// <summary>The untyped decoded envelope <c>data</c>. Everything the API returned is reachable here, typed or not.</summary>
    [JsonIgnore]
    public JsonElement Raw { get; internal set; }

    /// <summary>Transport-level facts about the response, or <see langword="null"/> for an instance built by hand.</summary>
    [JsonIgnore]
    public ResponseInfo? Response { get; internal set; }

    /// <summary>The request correlation id: the envelope <c>uuid</c>, else the <c>X-Request-Id</c> header.</summary>
    [JsonIgnore]
    public string? RequestId => Response?.RequestId;

    /// <summary><see langword="true"/> when the API answered from its idempotency store (<c>Idempotent-Replay: true</c>).</summary>
    [JsonIgnore]
    public bool Replayed => Response?.Replayed ?? false;

    internal virtual void AfterMaterialize()
    {
    }
}
