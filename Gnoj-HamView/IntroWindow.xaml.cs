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

            CbbEndOfGameRule.ItemsSource = GraphicTools.GetEndOfGameRuleDisplayValue();
            CbbPointsRule.ItemsSource = GraphicTools.GetInitialPointsRuleDisplayValue();
            CbbChronoSpeed.ItemsSource = GraphicTools.GetChronoDisplayValues();
            CbbCpuSpeed.ItemsSource = GraphicTools.GetCpuSpeedDisplayValues();

            LoadConfiguration();
        }

        private void BtnStart_Click(object sender, RoutedEventArgs e)
        {
            SaveConfiguration();

            Hide();

            var (save, error) = PlayerSavePivot.GetOrCreateSave();

            if (!string.IsNullOrWhiteSpace(error))
            {
                MessageBox.Show($"Something went wrong during the loading of player's stat file; statistics will not be saved.\n\nError details:\n{error}", "Gnoj-Ham - warning");
            }

            var ruleset = new RulePivot((InitialPointsRulePivot)CbbPointsRule.SelectedIndex,
                (EndOfGameRulePivot)CbbEndOfGameRule.SelectedIndex,
                ChkUseRedDoras.IsChecked == true,
                ChkUseNagashiMangan.IsChecked == true,
                ChkFourCpus.IsChecked == true,
                ChkDebugMode.IsChecked == true,
                ChkDiscardTip.IsChecked == true);

            new MainWindow(TxtPlayerName.Text, ruleset, save).ShowDialog();

            // The configuration might be updated in-game.
            LoadConfiguration();

            ShowDialog();
        }

        private void BtnQuit_Click(object sender, RoutedEventArgs e)
        {
            Environment.Exit(0);
        }

        private void LoadConfiguration()
        {
            // Rules
            CbbPointsRule.SelectedIndex = (int)RulePivot.Default.InitialPointsRule;
            CbbEndOfGameRule.SelectedIndex = (int)RulePivot.Default.EndOfGameRule;
            ChkUseRedDoras.IsChecked = RulePivot.Default.UseRedDoras;
            ChkUseNagashiMangan.IsChecked = RulePivot.Default.UseNagashiMangan;

            // Options as rules
            ChkDiscardTip.IsChecked = RulePivot.Default.DiscardTip;

            // Options
            TxtPlayerName.Text = Settings.Default.DefaultPlayerName;
            CbbChronoSpeed.SelectedIndex = Settings.Default.ChronoSpeed;
            CbbCpuSpeed.SelectedIndex = Settings.Default.CpuSpeed;
            ChkSounds.IsChecked = Settings.Default.PlaySounds;
            ChkAutoTsumoRon.IsChecked = Settings.Default.AutoCallMahjong;
            ChkAutoRiichiDiscard.IsChecked = Settings.Default.AutoDiscardAfterRiichi;

            // Dvelopment tools
            ChkDebugMode.IsChecked = false;
            ChkFourCpus.IsChecked = false;
        }

        private void SaveConfiguration()
        {
            Settings.Default.DefaultPlayerName = TxtPlayerName.Text;
            Settings.Default.ChronoSpeed = CbbChronoSpeed.SelectedIndex;
            Settings.Default.CpuSpeed = CbbCpuSpeed.SelectedIndex;
            Settings.Default.PlaySounds = ChkSounds.IsChecked == true;
            Settings.Default.AutoCallMahjong = ChkAutoTsumoRon.IsChecked == true;
            Settings.Default.AutoDiscardAfterRiichi = ChkAutoRiichiDiscard.IsChecked == true;

            Settings.Default.Save();
        }

        private void PlayerStatsHlk_Click(object sender, RoutedEventArgs e)
        {
            var (save, error) = PlayerSavePivot.GetOrCreateSave();

            if (!string.IsNullOrWhiteSpace(error))
            {
                MessageBox.Show($"Something went wrong during the loading of player's stat file; statistics will be empty.\n\nError details:\n{error}", "Gnoj-Ham - warning");
            }

            new PlayerSaveStatsWindow(save).ShowDialog();
        }

        private void BtnReset_Click(object sender, RoutedEventArgs e)
        {
            LoadConfiguration();
        }
    }
}
