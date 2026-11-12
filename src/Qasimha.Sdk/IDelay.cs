namespace Qasimha.Sdk;

/// <summary>Waits between retry attempts. Replace it in tests so retries complete instantly.</summary>
public interface IDelay
{
    /// <summary>Waits for <paramref name="duration"/>.</summary>
    /// <param name="duration">How long to wait.</param>
    /// <param name="cancellationToken">Cancels the wait.</param>
    Task DelayAsync(TimeSpan duration, CancellationToken cancellationToken);
}

/// <summary>The default <see cref="IDelay"/>, backed by <see cref="Task.Delay(TimeSpan, CancellationToken)"/>.</summary>
public sealed class TaskDelay : IDelay
{
    /// <summary>The shared instance.</summary>
    public static TaskDelay Instance { get; } = new();

    /// <inheritdoc />
    public Task DelayAsync(TimeSpan duration, CancellationToken cancellationToken) => Task.Delay(duration, cancellationToken);
}
