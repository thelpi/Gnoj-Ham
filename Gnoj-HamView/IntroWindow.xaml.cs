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

            LoadConfiguration();

            CbbEndOfGameRule.ItemsSource = GraphicTools.GetEndOfGameRuleDisplayValue();
            CbbPointsRule.ItemsSource = GraphicTools.GetInitialPointsRuleDisplayValue();
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
            CbbPointsRule.SelectedIndex = Settings.Default.InitialPointsRule;
            CbbEndOfGameRule.SelectedIndex = Settings.Default.EndOfGameRule;
            TxtPlayerName.Text = Settings.Default.DefaultPlayerName;
            ChkUseRedDoras.IsChecked = Settings.Default.DefaultUseRedDoras;
            ChkDebugMode.IsChecked = Settings.Default.DebugMode;
            ChkUseNagashiMangan.IsChecked = Settings.Default.UseNagashiMangan;
            ChkUseRenhou.IsChecked = Settings.Default.UseRenhou;
        }

        private void SaveConfiguration()
        {
            Settings.Default.InitialPointsRule = CbbPointsRule.SelectedIndex;
            Settings.Default.EndOfGameRule = CbbEndOfGameRule.SelectedIndex;
            Settings.Default.DefaultPlayerName = TxtPlayerName.Text;
            Settings.Default.DefaultUseRedDoras = ChkUseRedDoras.IsChecked == true;
            Settings.Default.DebugMode = ChkDebugMode.IsChecked == true;
            Settings.Default.UseNagashiMangan = ChkUseNagashiMangan.IsChecked == true;
            Settings.Default.UseRenhou = ChkUseRenhou.IsChecked == true;
            Settings.Default.Save();
        }

        #endregion Private methods
    }
}
