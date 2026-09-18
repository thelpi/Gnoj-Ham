using Gnoj_Ham_Library;

namespace Gnoj_Ham_ViewModel.Services;

/// <summary>
/// Reads and writes the player's statistics from/to wherever they are saved.
/// </summary>
public interface IPlayerStatisticsStorage
{
    /// <summary>
    /// Loads the player statistics, or creates a fresh instance if nothing is saved yet.
    /// </summary>
    /// <returns>Player statistics, and an error message if the load failed (the statistics are then empty).</returns>
    (PlayerStatisticsPivot stats, string? error) Load();

    /// <summary>
    /// Saves the player statistics.
    /// </summary>
    /// <param name="stats">Player statistics.</param>
    /// <returns>An error message if the save failed; <c>Null</c> otherwise.</returns>
    string? Save(PlayerStatisticsPivot stats);
}
