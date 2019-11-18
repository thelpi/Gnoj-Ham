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

            CbbCpuSpeed.SelectedIndex = Settings.Default.DefaultCpuSpeed;
            CbbPointsRule.SelectedIndex = Settings.Default.DefaultPointsRule;
            TxtPlayerName.Text = Settings.Default.DefaultPlayerName;
            ChkUseRedDoras.IsChecked = Settings.Default.DefaultUseRedDoras;
            ChkAutoTsumoRon.IsChecked = Settings.Default.DefaultAutoTsumoRon;
            ChkRiichiAutoDiscard.IsChecked = Settings.Default.DefaultRiichiAutoDiscard;
            ChkDebugMode.IsChecked = Settings.Default.DefaultDebugMode;
            ChkSortedDraw.IsChecked = Settings.Default.DefaultSortedDraw;
        }

        private void BtnStart_Click(object sender, RoutedEventArgs e)
        {
            Hide();
            new MainWindow(
                TxtPlayerName.Text,
                (InitialPointsRulePivot)CbbPointsRule.SelectedIndex,
                ChkUseRedDoras.IsChecked == true,
                (CpuSpeed)CbbCpuSpeed.SelectedIndex,
                ChkAutoTsumoRon.IsChecked == true,
                ChkRiichiAutoDiscard.IsChecked == true,
                ChkDebugMode.IsChecked == true,
                ChkSortedDraw.IsChecked == true
            ).ShowDialog();
            ShowDialog();
        }

        private void BtnQuit_Click(object sender, RoutedEventArgs e)
        {
            Environment.Exit(0);
        }
    }
}
