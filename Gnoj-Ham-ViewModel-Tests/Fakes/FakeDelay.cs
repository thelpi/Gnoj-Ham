using Gnoj_Ham_ViewModel.Services;

namespace Gnoj_Ham_ViewModel_Tests.Fakes;

/// <summary>
/// Never actually waits; records each requested delay so a test can assert on the game flow's pacing.
/// </summary>
internal sealed class FakeDelay : IDelay
{
    private readonly List<TimeSpan> _requestedDelays = new();

    public IReadOnlyList<TimeSpan> RequestedDelays => _requestedDelays;

    public Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _requestedDelays.Add(delay);
        return Task.CompletedTask;
    }
}
