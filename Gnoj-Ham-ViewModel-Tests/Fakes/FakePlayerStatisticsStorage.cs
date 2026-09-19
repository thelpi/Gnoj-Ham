using Gnoj_Ham_Library;
using Gnoj_Ham_ViewModel.Services;

namespace Gnoj_Ham_ViewModel_Tests.Fakes;

/// <summary>
/// Hands out the statistics (and load error) a test sets up, and counts the saves.
/// </summary>
internal sealed class FakePlayerStatisticsStorage : IPlayerStatisticsStorage
{
    public PlayerStatisticsPivot Stats { get; set; } = new();

    public string? LoadError { get; set; }

    public string? SaveError { get; set; }

    public int SaveCount { get; private set; }

    public (PlayerStatisticsPivot stats, string? error) Load() => (Stats, LoadError);

    public string? Save(PlayerStatisticsPivot stats)
    {
        SaveCount++;
        return SaveError;
    }
}
