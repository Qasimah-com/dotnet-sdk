namespace Qasimha.Sdk.Tests.Support;

internal sealed class FakeDelay : IDelay
{
    public List<TimeSpan> Waits { get; } = new();

    public Task DelayAsync(TimeSpan duration, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Waits.Add(duration);
        return Task.CompletedTask;
    }
}
