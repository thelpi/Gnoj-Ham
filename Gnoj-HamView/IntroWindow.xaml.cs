using System;
using System.Windows;
using Gnoj_Ham;
using Gnoj_HamView.Properties;

namespace Gnoj_HamView
{
    /// <summary>
    /// Interaction logic for IntroWindow.xaml
    /// </summary>
    public partial class IntroWindow : Window
    {
        #region Embedded properties

        private GamePivot _game;

        #endregion Embedded properties

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="currentGame">The current game; <c>Null</c> if new game.</param>
        public IntroWindow(GamePivot currentGame)
        {
            InitializeComponent();

            LoadConfiguration();

            _game = currentGame;

            if (_game != null)
            {
                CbbPointsRule.IsEnabled = false;
                CbbEndOfGameRule.IsEnabled = false;
                BtnStart.Content = "Resume";
                BtnQuit.Content = "Cancel";
            }
        }

        #region Page events

        private void BtnStart_Click(object sender, RoutedEventArgs e)
        {
            SaveConfiguration();

            if (_game != null)
            {
                _game.UpdateConfiguration(TxtPlayerName.Text,
                    ChkUseRedDoras.IsChecked == true,
                    ChkUseNagashiMangan.IsChecked == true,
                    ChkUseRenhou.IsChecked == true);
                Close();
            }
            else
            {
                Hide();

                new MainWindow(
                    TxtPlayerName.Text,
                    (InitialPointsRulePivot)CbbPointsRule.SelectedIndex,
                    (EndOfGameRulePivot)CbbEndOfGameRule.SelectedIndex,
                    ChkUseRedDoras.IsChecked == true,
                    ChkUseNagashiMangan.IsChecked == true,
                    ChkUseRenhou.IsChecked == true
                ).ShowDialog();

                // The configuration might be updated in-game.
                LoadConfiguration();

                ShowDialog();
            }
        }

        private void BtnQuit_Click(object sender, RoutedEventArgs e)
        {
            if (_game != null)
            {
                Close();
            }
            else
            {
                Environment.Exit(0);
            }
        }

        #endregion Page events

        #region Private methods

        private void LoadConfiguration()
        {
            CbbCpuSpeed.SelectedIndex = Settings.Default.CpuSpeed;
            CbbChrono.SelectedIndex = Settings.Default.ChronoSpeed;
            CbbPointsRule.SelectedIndex = Settings.Default.InitialPointsRule;
            CbbEndOfGameRule.SelectedIndex = Settings.Default.EndOfGameRule;
            TxtPlayerName.Text = Settings.Default.DefaultPlayerName;
            ChkUseRedDoras.IsChecked = Settings.Default.DefaultUseRedDoras;
            ChkAutoTsumoRon.IsChecked = Settings.Default.AutoCallMahjong;
            ChkRiichiAutoDiscard.IsChecked = Settings.Default.AutoDiscardAfterRiichi;
            ChkDebugMode.IsChecked = Settings.Default.DebugMode;
            ChkUseNagashiMangan.IsChecked = Settings.Default.UseNagashiMangan;
            ChkUseRenhou.IsChecked = Settings.Default.UseRenhou;
            ChkSounds.IsChecked = Settings.Default.PlaySounds;
        }

        private void SaveConfiguration()
        {
            Settings.Default.CpuSpeed = CbbCpuSpeed.SelectedIndex;
            Settings.Default.ChronoSpeed = CbbChrono.SelectedIndex;
            Settings.Default.InitialPointsRule = CbbPointsRule.SelectedIndex;
            Settings.Default.EndOfGameRule = CbbEndOfGameRule.SelectedIndex;
            Settings.Default.DefaultPlayerName = TxtPlayerName.Text;
            Settings.Default.DefaultUseRedDoras = ChkUseRedDoras.IsChecked == true;
            Settings.Default.AutoCallMahjong = ChkAutoTsumoRon.IsChecked == true;
            Settings.Default.AutoDiscardAfterRiichi = ChkRiichiAutoDiscard.IsChecked == true;
            Settings.Default.DebugMode = ChkDebugMode.IsChecked == true;
            Settings.Default.UseNagashiMangan = ChkUseNagashiMangan.IsChecked == true;
            Settings.Default.UseRenhou = ChkUseRenhou.IsChecked == true;
            Settings.Default.PlaySounds = ChkSounds.IsChecked == true;
            Settings.Default.Save();
        }

        #endregion Private methods
    }
}
