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

            // TODO : IConverter.
            CbbCpuSpeed.ItemsSource = Enum.GetValues(typeof(CpuSpeed));
            CbbPointsRule.ItemsSource = Enum.GetValues(typeof(InitialPointsRulePivot));

            CbbCpuSpeed.SelectedIndex = Settings.Default.DefaultCpuSpeed;
            CbbPointsRule.SelectedIndex = Settings.Default.DefaultPointsRule;
            TxtPlayerName.Text = Settings.Default.DefaultPlayerName;
            ChkUseRedDoras.IsChecked = Settings.Default.DefaultUseRedDoras;
        }

        private void BtnStart_Click(object sender, RoutedEventArgs e)
        {
            Hide();
            new MainWindow(
                TxtPlayerName.Text,
                (InitialPointsRulePivot)CbbPointsRule.SelectedItem,
                ChkUseRedDoras.IsChecked == true,
                (CpuSpeed)CbbCpuSpeed.SelectedItem
            ).ShowDialog();
            ShowDialog();
        }

        private void BtnQuit_Click(object sender, RoutedEventArgs e)
        {
            Environment.Exit(0);
        }
    }
}
