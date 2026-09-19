using Gnoj_Ham_ViewModel.Services;

namespace Gnoj_Ham_ViewModel_Tests.Fakes;

/// <summary>
/// Waits until the test says the time is up (or the wait is cancelled): for testing what happens while
/// a timer is running, and once it goes off.
/// </summary>
internal sealed class ManualDelay : IDelay
{
    private readonly List<(TimeSpan delay, TaskCompletionSource source)> _waits = new();

    /// <summary>
    /// The waits still going, in the order they were started.
    /// </summary>
    public IReadOnlyList<TimeSpan> PendingDelays
    {
        get
        {
            lock (_waits)
            {
                return _waits.Select(w => w.delay).ToList();
            }
        }
    }

    public Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken = default)
    {
        // Continuations run inline when the wait ends, so that what follows a timer going off is done
        // by the time the test moves on.
        var source = new TaskCompletionSource();
        lock (_waits)
        {
            _waits.Add((delay, source));
        }

        cancellationToken.Register(() =>
        {
            lock (_waits)
            {
                _waits.RemoveAll(w => w.source == source);
            }
            source.TrySetCanceled(cancellationToken);
        });

        return source.Task;
    }

    /// <summary>
    /// Ends every wait still going.
    /// </summary>
    public void ElapseAll()
    {
        List<(TimeSpan delay, TaskCompletionSource source)> waits;
        lock (_waits)
        {
            waits = _waits.ToList();
            _waits.Clear();
        }

        foreach (var (_, source) in waits)
        {
            source.TrySetResult();
        }
    }
}
