using Gnoj_Ham_Library;

namespace Gnoj_Ham_ViewModel;

/// <summary>
/// The player's saved statistics, as the strings the statistics window displays.
/// </summary>
public sealed class PlayerSaveStatsViewModel
{
    private const string NoValue = "N/A";
    private const string DateFormat = "dd/MM/yyyy";

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="stats">Player statistics.</param>
    public PlayerSaveStatsViewModel(PlayerStatisticsPivot stats)
    {
        FirstGame = stats.FirstGame?.ToString(DateFormat) ?? NoValue;
        LatestGame = stats.LastGame?.ToString(DateFormat) ?? NoValue;
        GamesCount = stats.GameCount;
        RoundsCount = stats.RoundCount;

        BankruptCount = stats.BankruptCount;
        OpenedHandsCount = stats.OpenedHandCount;
        RiichiCount = stats.RiichiCount;
        RonCount = stats.RonCount;
        TsumoCount = stats.TsumoCount;
        YakumanCount = stats.YakumanCount;

        GamesPercent = $"1er : {ToPercent(stats.ByPositionCount[0], stats.GameCount)}\n" +
            $"2ème : {ToPercent(stats.ByPositionCount[1], stats.GameCount)}\n" +
            $"3ème : {ToPercent(stats.ByPositionCount[2], stats.GameCount)}\n" +
            $"4ème : {ToPercent(stats.ByPositionCount[3], stats.GameCount)}";
        BankruptPercent = ToPercent(stats.BankruptCount, stats.GameCount);
        OpenedHandsPercent = ToPercent(stats.OpenedHandCount, stats.RoundCount);
        RiichiPercent = ToPercent(stats.RiichiCount, stats.RoundCount);
        RonPercent = ToPercent(stats.RonCount, stats.RoundCount);
        TsumoPercent = ToPercent(stats.TsumoCount, stats.RoundCount);
        YakumanPercent = ToPercent(stats.YakumanCount, stats.RoundCount);
    }

    /// <summary>Date of the first game, or "N/A".</summary>
    public string FirstGame { get; }

    /// <summary>Date of the latest complete game, or "N/A".</summary>
    public string LatestGame { get; }

    /// <summary>Number of games.</summary>
    public int GamesCount { get; }

    /// <summary>Number of rounds.</summary>
    public int RoundsCount { get; }

    /// <summary>Number of bankruptcies.</summary>
    public int BankruptCount { get; }

    /// <summary>Number of rounds won with an opened hand.</summary>
    public int OpenedHandsCount { get; }

    /// <summary>Number of riichi calls.</summary>
    public int RiichiCount { get; }

    /// <summary>Number of ron.</summary>
    public int RonCount { get; }

    /// <summary>Number of tsumo.</summary>
    public int TsumoCount { get; }

    /// <summary>Number of yakuman.</summary>
    public int YakumanCount { get; }

    /// <summary>Finishing position breakdown, one line per position.</summary>
    public string GamesPercent { get; }

    /// <summary>Bankruptcies, as a share of games.</summary>
    public string BankruptPercent { get; }

    /// <summary>Opened hands, as a share of rounds.</summary>
    public string OpenedHandsPercent { get; }

    /// <summary>Riichi calls, as a share of rounds.</summary>
    public string RiichiPercent { get; }

    /// <summary>Ron, as a share of rounds.</summary>
    public string RonPercent { get; }

    /// <summary>Tsumo, as a share of rounds.</summary>
    public string TsumoPercent { get; }

    /// <summary>Yakuman, as a share of rounds.</summary>
    public string YakumanPercent { get; }

    private static string ToPercent(int baseValue, int totalValue)
    {
        var percentValue = totalValue == 0
            ? 0
            : (int)Math.Round(baseValue / (decimal)totalValue * 100);

        return $"{percentValue} %";
    }
}
