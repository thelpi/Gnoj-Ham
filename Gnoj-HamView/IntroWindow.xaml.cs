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
        }

        #region Page events

        private void BtnStart_Click(object sender, RoutedEventArgs e)
        {
            SaveConfiguration();

            Hide();
            new MainWindow(
                TxtPlayerName.Text,
                (InitialPointsRulePivot)CbbPointsRule.SelectedIndex,
                ChkUseRedDoras.IsChecked == true,
                (CpuSpeed)CbbCpuSpeed.SelectedIndex,
                ChkAutoTsumoRon.IsChecked == true,
                ChkRiichiAutoDiscard.IsChecked == true,
                ChkDebugMode.IsChecked == true,
                ChkSortedDraw.IsChecked == true,
                ChkUseNagashiMangan.IsChecked == true,
                ChkUseRenhou.IsChecked == true
            ).ShowDialog();
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
            CbbCpuSpeed.SelectedIndex = Settings.Default.DefaultCpuSpeed;
            CbbPointsRule.SelectedIndex = Settings.Default.DefaultPointsRule;
            TxtPlayerName.Text = Settings.Default.DefaultPlayerName;
            ChkUseRedDoras.IsChecked = Settings.Default.DefaultUseRedDoras;
            ChkAutoTsumoRon.IsChecked = Settings.Default.DefaultAutoTsumoRon;
            ChkRiichiAutoDiscard.IsChecked = Settings.Default.DefaultRiichiAutoDiscard;
            ChkDebugMode.IsChecked = Settings.Default.DefaultDebugMode;
            ChkSortedDraw.IsChecked = Settings.Default.DefaultSortedDraw;
            ChkUseNagashiMangan.IsChecked = Settings.Default.DefaultUseNagashiMangan;
            ChkUseRenhou.IsChecked = Settings.Default.DefaultUseRenhou;
        }

        private void SaveConfiguration()
        {
            Settings.Default.DefaultCpuSpeed = CbbCpuSpeed.SelectedIndex;
            Settings.Default.DefaultPointsRule = CbbPointsRule.SelectedIndex;
            Settings.Default.DefaultPlayerName = TxtPlayerName.Text;
            Settings.Default.DefaultUseRedDoras = ChkUseRedDoras.IsChecked == true;
            Settings.Default.DefaultAutoTsumoRon = ChkAutoTsumoRon.IsChecked == true;
            Settings.Default.DefaultRiichiAutoDiscard = ChkRiichiAutoDiscard.IsChecked == true;
            Settings.Default.DefaultDebugMode = ChkDebugMode.IsChecked == true;
            Settings.Default.DefaultSortedDraw = ChkSortedDraw.IsChecked == true;
            Settings.Default.DefaultUseNagashiMangan = ChkUseNagashiMangan.IsChecked == true;
            Settings.Default.DefaultUseRenhou = ChkUseRenhou.IsChecked == true;
            Settings.Default.Save();
        }

        #endregion Private methods
    }
}
