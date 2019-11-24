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
        /// Chrono.
        /// </summary>
        internal ChronoPivot Chrono { get; private set; }
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
        /// <summary>
        /// Sounds y/n.
        /// </summary>
        internal bool Sounds { get; private set; }

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
            CpuSpeed = (CpuSpeedPivot)CbbCpuSpeed.SelectedIndex;
            Chrono = (ChronoPivot)CbbChrono.SelectedIndex;
            AutoTsumoRon = ChkAutoTsumoRon.IsChecked == true;
            RiichiAutoDiscard = ChkRiichiAutoDiscard.IsChecked == true;
            DebugMode = ChkDebugMode.IsChecked == true;
            Sounds = ChkSounds.IsChecked == true;

            SaveConfiguration();

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
                    (EndOfGameRulePivot)CbbEndOfGameRule.SelectedIndex,
                    ChkUseRedDoras.IsChecked == true,
                    CpuSpeed,
                    Chrono,
                    AutoTsumoRon,
                    RiichiAutoDiscard,
                    DebugMode,
                    ChkSortedDraw.IsChecked == true,
                    ChkUseNagashiMangan.IsChecked == true,
                    ChkUseRenhou.IsChecked == true,
                    Sounds
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
            CbbChrono.SelectedIndex = Settings.Default.DefaultChrono;
            CbbPointsRule.SelectedIndex = Settings.Default.DefaultPointsRule;
            CbbEndOfGameRule.SelectedIndex = Settings.Default.DefaultEndOfGameRule;
            TxtPlayerName.Text = Settings.Default.DefaultPlayerName;
            ChkUseRedDoras.IsChecked = Settings.Default.DefaultUseRedDoras;
            ChkAutoTsumoRon.IsChecked = Settings.Default.DefaultAutoTsumoRon;
            ChkRiichiAutoDiscard.IsChecked = Settings.Default.DefaultRiichiAutoDiscard;
            ChkDebugMode.IsChecked = Settings.Default.DefaultDebugMode;
            ChkSortedDraw.IsChecked = Settings.Default.DefaultSortedDraw;
            ChkUseNagashiMangan.IsChecked = Settings.Default.DefaultUseNagashiMangan;
            ChkUseRenhou.IsChecked = Settings.Default.DefaultUseRenhou;
            ChkSounds.IsChecked = Settings.Default.DefaultSounds;
        }

        private void SaveConfiguration()
        {
            Settings.Default.DefaultCpuSpeed = (int)CpuSpeed;
            Settings.Default.DefaultChrono = (int)Chrono;
            Settings.Default.DefaultPointsRule = CbbPointsRule.SelectedIndex;
            Settings.Default.DefaultEndOfGameRule = CbbEndOfGameRule.SelectedIndex;
            Settings.Default.DefaultPlayerName = TxtPlayerName.Text;
            Settings.Default.DefaultUseRedDoras = ChkUseRedDoras.IsChecked == true;
            Settings.Default.DefaultAutoTsumoRon = AutoTsumoRon;
            Settings.Default.DefaultRiichiAutoDiscard = RiichiAutoDiscard;
            Settings.Default.DefaultDebugMode = DebugMode;
            Settings.Default.DefaultSortedDraw = ChkSortedDraw.IsChecked == true;
            Settings.Default.DefaultUseNagashiMangan = ChkUseNagashiMangan.IsChecked == true;
            Settings.Default.DefaultUseRenhou = ChkUseRenhou.IsChecked == true;
            Settings.Default.DefaultSounds = Sounds;
            Settings.Default.Save();
        }

        #endregion Private methods
    }
}
