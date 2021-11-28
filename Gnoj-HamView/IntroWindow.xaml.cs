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
        /// <summary>
        /// Constructor.
        /// </summary>
        public IntroWindow()
        {
            InitializeComponent();

#if DEBUG
            GrbDebugOptions.Visibility = Visibility.Visible;
#endif

            LoadConfiguration();

            CbbEndOfGameRule.ItemsSource = GraphicTools.GetEndOfGameRuleDisplayValue();
            CbbPointsRule.ItemsSource = GraphicTools.GetInitialPointsRuleDisplayValue();
            CbbChronoSpeed.ItemsSource = GraphicTools.GetChronoDisplayValues();
            CbbCpuSpeed.ItemsSource = GraphicTools.GetCpuSpeedDisplayValues();
        }

        #region Page events

        private void BtnStart_Click(object sender, RoutedEventArgs e)
        {
            SaveConfiguration();

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

        private void BtnQuit_Click(object sender, RoutedEventArgs e)
        {
            Environment.Exit(0);
        }

        #endregion Page events

        #region Private methods

        private void LoadConfiguration()
        {
            // Rules
            CbbPointsRule.SelectedIndex = Settings.Default.InitialPointsRule;
            CbbEndOfGameRule.SelectedIndex = Settings.Default.EndOfGameRule;
            ChkUseRedDoras.IsChecked = Settings.Default.DefaultUseRedDoras;
            ChkUseNagashiMangan.IsChecked = Settings.Default.UseNagashiMangan;
            ChkUseRenhou.IsChecked = Settings.Default.UseRenhou;

            // Options
            TxtPlayerName.Text = Settings.Default.DefaultPlayerName;
            CbbChronoSpeed.SelectedIndex = Settings.Default.ChronoSpeed;
            CbbCpuSpeed.SelectedIndex = Settings.Default.CpuSpeed;
            ChkSounds.IsChecked = Settings.Default.PlaySounds;
            ChkAutoTsumoRon.IsChecked = Settings.Default.AutoCallMahjong;
            ChkAutoRiichiDiscard.IsChecked = Settings.Default.AutoDiscardAfterRiichi;

            // Debug
#if DEBUG
            ChkDebugMode.IsChecked = Settings.Default.DebugMode;
#endif
        }

        private void SaveConfiguration()
        {
            // Rules
            Settings.Default.InitialPointsRule = CbbPointsRule.SelectedIndex;
            Settings.Default.EndOfGameRule = CbbEndOfGameRule.SelectedIndex;
            Settings.Default.DefaultUseRedDoras = ChkUseRedDoras.IsChecked == true;
            Settings.Default.UseNagashiMangan = ChkUseNagashiMangan.IsChecked == true;
            Settings.Default.UseRenhou = ChkUseRenhou.IsChecked == true;

            // Options
            Settings.Default.DefaultPlayerName = TxtPlayerName.Text;
            Settings.Default.ChronoSpeed = CbbChronoSpeed.SelectedIndex;
            Settings.Default.CpuSpeed = CbbCpuSpeed.SelectedIndex;
            Settings.Default.PlaySounds = ChkSounds.IsChecked == true;
            Settings.Default.AutoCallMahjong = ChkAutoTsumoRon.IsChecked == true;
            Settings.Default.AutoDiscardAfterRiichi = ChkAutoRiichiDiscard.IsChecked == true;

            // Debug
#if DEBUG
            Settings.Default.DebugMode = ChkDebugMode.IsChecked == true;
#endif

            Settings.Default.Save();
        }

        #endregion Private methods
    }
}
