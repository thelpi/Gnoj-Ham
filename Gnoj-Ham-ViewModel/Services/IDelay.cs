namespace Gnoj_Ham_ViewModel.Services;

/// <summary>
/// Waits for a duration without blocking the UI. Every pause in the game flow (CPU speed, call
/// announcements, the human decision timer) goes through this, so tests can skip or record them
/// instead of actually waiting.
/// </summary>
public interface IDelay
{
    /// <summary>
    /// Waits for the specified duration.
    /// </summary>
    /// <param name="delay">The duration to wait.</param>
    /// <param name="cancellationToken">Cancels the wait.</param>
    /// <returns>A task completing once the duration has elapsed.</returns>
    /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> was cancelled.</exception>
    Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken = default);
}
