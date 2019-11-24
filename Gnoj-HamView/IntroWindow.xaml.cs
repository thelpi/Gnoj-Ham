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

        /// <summary>
        /// CPU speed.
        /// </summary>
        internal CpuSpeedPivot CpuSpeed { get; private set; }
        /// <summary>
        /// Auto call for tsumo and ron y/n.
        /// </summary>
        internal bool AutoTsumoRon { get; private set; }
        /// <summary>
        /// Riichi auto-discard y/n.
        /// </summary>
        internal bool RiichiAutoDiscard { get; private set; }
        /// <summary>
        /// Debug mode y/n.
        /// </summary>
        internal bool DebugMode { get; private set; }

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
                BtnStart.Content = "Resume";
                BtnQuit.Content = "Cancel";
            }
        }

        #region Page events

        private void BtnStart_Click(object sender, RoutedEventArgs e)
        {
            SaveConfiguration();

            CpuSpeed = (CpuSpeedPivot)CbbCpuSpeed.SelectedIndex;
            AutoTsumoRon = ChkAutoTsumoRon.IsChecked == true;
            RiichiAutoDiscard = ChkRiichiAutoDiscard.IsChecked == true;
            DebugMode = ChkDebugMode.IsChecked == true;

            if (_game != null)
            {
                _game.UpdateConfiguration(TxtPlayerName.Text,
                    ChkUseRedDoras.IsChecked == true,
                    ChkSortedDraw.IsChecked == true,
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
                    ChkUseRedDoras.IsChecked == true,
                    CpuSpeed,
                    AutoTsumoRon,
                    RiichiAutoDiscard,
                    DebugMode,
                    ChkSortedDraw.IsChecked == true,
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
