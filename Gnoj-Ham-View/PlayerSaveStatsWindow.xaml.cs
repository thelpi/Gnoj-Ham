using System.Windows;
using Gnoj_Ham_Library;

namespace Gnoj_Ham_View;

/// <summary>
/// Logique d'interaction pour PlayerSaveStatsWindow.xaml
/// </summary>
public partial class PlayerSaveStatsWindow : Window
{
    private const string NoValue = "N/A";
    private const string DateFormat = "dd/MM/yyyy";

    /// <summary>
    /// Ctor.
    /// </summary>
    /// <param name="stats">Player statistics.</param>
    public PlayerSaveStatsWindow(PlayerStatisticsPivot stats)
    {
        InitializeComponent();

        // General
        LblFirstGame.Content = stats.FirstGame?.ToString(DateFormat) ?? NoValue;
        LblLatestGame.Content = stats.LastGame?.ToString(DateFormat) ?? NoValue;
        LblGamesCount.Content = stats.GameCount;
        LblRoundsCount.Content = stats.RoundCount;

        // Raw stats
        LblBankruptCount.Content = stats.BankruptCount;
        LblOpenedHands.Content = stats.OpenedHandCount;
        LblRiichiCount.Content = stats.RiichiCount;
        LblRonCount.Content = stats.RonCount;
        LblTsumoCount.Content = stats.TsumoCount;
        LblYakumanCount.Content = stats.YakumanCount;

        // Percentage stats
        LblGamesPercent.Content = $"1er : {ToPercent(stats.ByPositionCount[0], stats.GameCount)}\n" +
            $"2ème : {ToPercent(stats.ByPositionCount[1], stats.GameCount)}\n" +
            $"3ème : {ToPercent(stats.ByPositionCount[2], stats.GameCount)}\n" +
            $"4ème : {ToPercent(stats.ByPositionCount[3], stats.GameCount)}";
        LblBankruptPercent.Content = ToPercent(stats.BankruptCount, stats.GameCount);
        LblOpenedHandsPercent.Content = ToPercent(stats.OpenedHandCount, stats.RoundCount);
        LblRiichiPercent.Content = ToPercent(stats.RiichiCount, stats.RoundCount);
        LblRonPercent.Content = ToPercent(stats.RonCount, stats.RoundCount);
        LblTsumoPercent.Content = ToPercent(stats.TsumoCount, stats.RoundCount);
        LblYakumanPercent.Content = ToPercent(stats.YakumanCount, stats.RoundCount);
    }

    private static string ToPercent(int baseValue, int totalValue)
    {
        var percentValue = totalValue == 0
            ? 0
            : (int)Math.Round(baseValue / (decimal)totalValue * 100);

        return $"{percentValue} %";
    }
}
