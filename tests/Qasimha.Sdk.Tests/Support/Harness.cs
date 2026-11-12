namespace Qasimha.Sdk.Tests.Support;

internal sealed record LogEntry(QasimhaLogLevel Level, string Message, Dictionary<string, object?> Fields);

internal sealed class Harness : IDisposable
{
    public const string ApiKey = "sk_test_abc123XYZ";

    public const string WebhookSecret = "whsec_test_0123456789abcdef";

    public const string PublishableKey = "pk_test_pub456DEF";

    public Harness(Action<QasimhaClientOptions>? configure = null)
    {
        Options = new QasimhaClientOptions
        {
            ApiKey = ApiKey,
            WebhookSecret = WebhookSecret,
            PublishableKey = PublishableKey,
            Delay = Delay,
            Logger = (level, message, fields) => Logs.Add(new LogEntry(level, message, new Dictionary<string, object?>(fields))),
            Clock = () => Now,
        };
        configure?.Invoke(Options);
        Client = new QasimhaClient(Options, Handler);
    }

    public FakeHandler Handler { get; } = new();

    public FakeDelay Delay { get; } = new();

    public List<LogEntry> Logs { get; } = new();

    public DateTimeOffset Now { get; set; } = new(2026, 9, 25, 9, 0, 0, TimeSpan.Zero);

    public QasimhaClientOptions Options { get; }

    public QasimhaClient Client { get; }

    public CapturedRequest LastRequest => Handler.Requests[^1];

    public void Dispose() => Client.Dispose();
}
