namespace Gnoj_Ham_ViewModel.Services;

/// <summary>
/// Waits for real, without blocking the thread.
/// </summary>
public sealed class TaskDelay : IDelay
{
    /// <inheritdoc />
    public Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken = default)
        => Task.Delay(delay, cancellationToken);
}
