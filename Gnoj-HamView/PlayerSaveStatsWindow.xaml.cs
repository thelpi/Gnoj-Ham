using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Gnoj_Ham;

namespace Gnoj_HamView
{
    /// <summary>
    /// Logique d'interaction pour PlayerSaveStatsWindow.xaml
    /// </summary>
    public partial class PlayerSaveStatsWindow : Window
    {
        private const string NoValue = "N/A";
        private const string DateFormat = "yyyy-MM-dd";

        /// <summary>
        /// Ctor.
        /// </summary>
        /// <param name="playerSavePivot">Player save file.</param>
        public PlayerSaveStatsWindow(PlayerSavePivot playerSavePivot)
        {
            InitializeComponent();

            // General
            LblFirstGame.Content = playerSavePivot.FirstGame?.ToString(DateFormat) ?? NoValue;
            LblLatestGame.Content = playerSavePivot.LastGame?.ToString(DateFormat) ?? NoValue;
            LblGamesCount.Content = playerSavePivot.GameCount;
            LblRoundsCount.Content = playerSavePivot.RoundCount;

            // Raw stats
            LblBankruptCount.Content = playerSavePivot.BankruptCount;
            LblOpenedHands.Content = playerSavePivot.OpenedHandCount;
            LblRiichiCount.Content = playerSavePivot.RiichiCount;
            LblRonCount.Content = playerSavePivot.RonCount;
            LblTsumoCount.Content = playerSavePivot.TsumoCount;
            LblYakumanCount.Content = playerSavePivot.YakumanCount;

            // Percentage stats
            LblGamesPercent.Content = $"1th: {ToPercent(playerSavePivot.ByPositionCount[0], playerSavePivot.GameCount)}\n" +
                $"2nd: {ToPercent(playerSavePivot.ByPositionCount[1], playerSavePivot.GameCount)}\n" +
                $"3th: {ToPercent(playerSavePivot.ByPositionCount[2], playerSavePivot.GameCount)}\n" +
                $"4th: {ToPercent(playerSavePivot.ByPositionCount[3], playerSavePivot.GameCount)}";
            LblBankruptPercent.Content = ToPercent(playerSavePivot.BankruptCount, playerSavePivot.GameCount);
            LblOpenedHandsPercent.Content = ToPercent(playerSavePivot.OpenedHandCount, playerSavePivot.RoundCount);
            LblRiichiPercent.Content = ToPercent(playerSavePivot.RiichiCount, playerSavePivot.RoundCount);
            LblRonPercent.Content = ToPercent(playerSavePivot.RonCount, playerSavePivot.RoundCount);
            LblTsumoPercent.Content = ToPercent(playerSavePivot.TsumoCount, playerSavePivot.RoundCount);
            LblYakumanPercent.Content = ToPercent(playerSavePivot.YakumanCount, playerSavePivot.RoundCount);
        }

        private static string ToPercent(int baseValue, int totalValue)
        {
            var percentValue = totalValue == 0
                ? 0
                : (int)Math.Round((baseValue / (decimal)totalValue) * 100);

            return $"{percentValue} %";
        }
    }
}
