using Gnoj_Ham_ViewModel.Services;

namespace Gnoj_Ham_ViewModel_Tests.Fakes;

/// <summary>
/// Plays nothing; counts the ticks.
/// </summary>
internal sealed class FakeSoundService : ISoundService
{
    public int TickCount { get; private set; }

    public void PlayTick() => TickCount++;
}
