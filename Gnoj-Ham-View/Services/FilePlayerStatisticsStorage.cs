using Gnoj_Ham_Library;
using Gnoj_Ham_ViewModel.Services;

namespace Gnoj_Ham_View.Services;

/// <summary>
/// The player statistics, kept in the local (encrypted) save file: see <see cref="PlayerSaveStorage"/>.
/// </summary>
internal sealed class FilePlayerStatisticsStorage : IPlayerStatisticsStorage
{
    /// <inheritdoc />
    public (PlayerStatisticsPivot stats, string? error) Load() => PlayerSaveStorage.Load();

    /// <inheritdoc />
    public string? Save(PlayerStatisticsPivot stats) => PlayerSaveStorage.Save(stats);
}
