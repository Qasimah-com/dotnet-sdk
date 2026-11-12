namespace Qasimha.Sdk.Http;

internal sealed class ClientConfiguration
{
    public const string ProductionBaseUrl = "https://api.qasimha.com";

    public const string SandboxBaseUrl = "https://api-uat.qasimha.com";

    private const string TestPrefix = "sk_test_";

    private const string LivePrefix = "sk_live_";

    private const string PublishableTestPrefix = "pk_test_";

    private const string PublishableLivePrefix = "pk_live_";

    private ClientConfiguration()
    {
    }

    public string ApiKey { get; private init; } = string.Empty;

    public string MaskedKey { get; private init; } = string.Empty;

    public string? PublishableKey { get; private init; }

    public string? MaskedPublishableKey { get; private init; }

    public QasimhaEnvironment Environment { get; private init; }

    public string BaseUrl { get; private init; } = string.Empty;

    public TimeSpan Timeout { get; private init; }

    public int MaxRetries { get; private init; }

    public string? WebhookSecret { get; private init; }

    public string Language { get; private init; } = "en";

    public string UserAgent { get; private init; } = string.Empty;

    public QasimhaLogger? Logger { get; private init; }

    public IDelay Delay { get; private init; } = TaskDelay.Instance;

    public Func<DateTimeOffset> Clock { get; private init; } = () => DateTimeOffset.UtcNow;

    public static ClientConfiguration From(QasimhaClientOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var apiKey = options.ApiKey?.Trim() ?? string.Empty;
        if (!IsWellFormedKey(apiKey, TestPrefix, LivePrefix))
        {
            throw new ConfigurationException("ApiKey is required and must match ^sk_(test|live)_[A-Za-z0-9]+$.");
        }

        var keyEnvironment = apiKey.StartsWith(LivePrefix, StringComparison.Ordinal)
            ? QasimhaEnvironment.Production
            : QasimhaEnvironment.Sandbox;

        var publishableKey = string.IsNullOrWhiteSpace(options.PublishableKey)
            ? null
            : ValidatePublishableKey(options.PublishableKey, keyEnvironment);

        string baseUrl;
        QasimhaEnvironment environment;
        if (!string.IsNullOrWhiteSpace(options.BaseUrl))
        {
            baseUrl = NormalizeBaseUrl(options.BaseUrl);
            environment = options.Environment ?? keyEnvironment;
        }
        else
        {
            if (options.Environment is { } requested && requested != keyEnvironment)
            {
                throw new ConfigurationException(
                    $"Environment '{requested}' contradicts the API key prefix, which selects '{keyEnvironment}'.");
            }

            environment = keyEnvironment;
            baseUrl = environment == QasimhaEnvironment.Production ? ProductionBaseUrl : SandboxBaseUrl;
        }

        if (options.Timeout <= TimeSpan.Zero)
        {
            throw new ConfigurationException("Timeout must be greater than zero.");
        }

        if (options.MaxRetries is < 0 or > 5)
        {
            throw new ConfigurationException("MaxRetries must be between 0 and 5.");
        }

        return new ClientConfiguration
        {
            ApiKey = apiKey,
            MaskedKey = Mask(apiKey),
            PublishableKey = publishableKey,
            MaskedPublishableKey = publishableKey is null ? null : Mask(publishableKey),
            Environment = environment,
            BaseUrl = baseUrl,
            Timeout = options.Timeout,
            MaxRetries = options.MaxRetries,
            WebhookSecret = string.IsNullOrWhiteSpace(options.WebhookSecret) ? null : options.WebhookSecret,
            Language = NormalizeLanguage(options.Language),
            UserAgent = Http.UserAgent.Build(options.UserAgentSuffix),
            Logger = options.Logger,
            Delay = options.Delay ?? TaskDelay.Instance,
            Clock = options.Clock ?? (() => DateTimeOffset.UtcNow),
        };
    }

    public void Log(QasimhaLogLevel level, string message, IReadOnlyDictionary<string, object?> fields) =>
        Logger?.Invoke(level, message, fields);

    public string ResolvePublishableKey(string? perCall)
    {
        if (!string.IsNullOrWhiteSpace(perCall))
        {
            return ValidatePublishableKey(perCall, KeyEnvironment);
        }

        return PublishableKey
            ?? throw new ConfigurationException(
                "PublishableKey is required for this call: set QasimhaClientOptions.PublishableKey or pass GetPlansOptions.PublishableKey.");
    }

    private QasimhaEnvironment KeyEnvironment =>
        ApiKey.StartsWith(LivePrefix, StringComparison.Ordinal) ? QasimhaEnvironment.Production : QasimhaEnvironment.Sandbox;

    private static string ValidatePublishableKey(string value, QasimhaEnvironment keyEnvironment)
    {
        var key = value.Trim();
        if (!IsWellFormedKey(key, PublishableTestPrefix, PublishableLivePrefix))
        {
            throw new ConfigurationException("PublishableKey must match ^pk_(test|live)_[A-Za-z0-9]+$.");
        }

        var publishableEnvironment = key.StartsWith(PublishableLivePrefix, StringComparison.Ordinal)
            ? QasimhaEnvironment.Production
            : QasimhaEnvironment.Sandbox;
        if (publishableEnvironment != keyEnvironment)
        {
            throw new ConfigurationException(
                $"PublishableKey selects '{publishableEnvironment}' but the API key prefix selects '{keyEnvironment}'.");
        }

        return key;
    }

    private static bool IsWellFormedKey(string key, string testPrefix, string livePrefix)
    {
        string prefix;
        if (key.StartsWith(testPrefix, StringComparison.Ordinal))
        {
            prefix = testPrefix;
        }
        else if (key.StartsWith(livePrefix, StringComparison.Ordinal))
        {
            prefix = livePrefix;
        }
        else
        {
            return false;
        }

        if (key.Length == prefix.Length)
        {
            return false;
        }

        for (var i = prefix.Length; i < key.Length; i++)
        {
            var c = key[i];
            if (c is not ((>= 'A' and <= 'Z') or (>= 'a' and <= 'z') or (>= '0' and <= '9')))
            {
                return false;
            }
        }

        return true;
    }

    private static string Mask(string key)
    {
        var prefixLength = key.IndexOf('_', TestPrefix.Length - 1) + 1;
        var prefix = key.AsSpan(0, prefixLength);
        var rest = key.AsSpan(prefixLength);
        return rest.Length >= 4
            ? string.Concat(prefix, "…", rest.Slice(rest.Length - 4))
            : string.Concat(prefix, "…");
    }

    private static string NormalizeBaseUrl(string value)
    {
        if (!Uri.TryCreate(value.Trim(), UriKind.Absolute, out var uri))
        {
            throw new ConfigurationException("BaseUrl must be an absolute https:// URL.");
        }

        var isLoopback = string.Equals(uri.Host, "localhost", StringComparison.OrdinalIgnoreCase)
            || string.Equals(uri.Host, "127.0.0.1", StringComparison.Ordinal);
        var isHttps = string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase);
        var isHttp = string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase);

        if (!isHttps && !(isHttp && isLoopback))
        {
            throw new ConfigurationException("BaseUrl must use https:// (http:// is accepted only for localhost and 127.0.0.1).");
        }

        return uri.GetLeftPart(UriPartial.Path).TrimEnd('/');
    }

    public static string NormalizeLanguage(string? language)
    {
        if (string.IsNullOrWhiteSpace(language))
        {
            return "en";
        }

        var value = language.Trim();
        if (string.Equals(value, "en", StringComparison.OrdinalIgnoreCase))
        {
            return "en";
        }

        if (string.Equals(value, "ar", StringComparison.OrdinalIgnoreCase))
        {
            return "ar";
        }

        throw new ConfigurationException("Language must be \"en\" or \"ar\".");
    }
}
