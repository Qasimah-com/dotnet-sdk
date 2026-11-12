namespace Qasimha.Sdk;

/// <summary>Severity of a log entry emitted by the SDK.</summary>
public enum QasimhaLogLevel
{
    /// <summary>Per-request diagnostics.</summary>
    Debug,

    /// <summary>Notable but expected events, such as a replayed create.</summary>
    Info,

    /// <summary>A failure the SDK is about to retry.</summary>
    Warn,

    /// <summary>A failure surfaced to the caller as an exception.</summary>
    Error,
}

/// <summary>
/// Receives the SDK's log entries. <paramref name="fields"/> carries <c>method</c>, <c>path</c>,
/// <c>status</c>, <c>durationMs</c>, <c>requestId</c>, <c>attempt</c> and <c>code</c>; it never
/// carries the API key, the webhook secret, query strings or consumer data.
/// </summary>
/// <param name="level">Severity of the entry.</param>
/// <param name="message">A short, fixed description of the event.</param>
/// <param name="fields">Structured context for the entry.</param>
public delegate void QasimhaLogger(QasimhaLogLevel level, string message, IReadOnlyDictionary<string, object?> fields);
