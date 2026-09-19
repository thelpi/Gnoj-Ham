using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using Gnoj_Ham_View.Services;
using Gnoj_Ham_ViewModel;
using Gnoj_Ham_ViewModel.Services;

namespace Gnoj_Ham_View;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private const string WINDOW_TITLE = "Gnoj-Ham";

    // The table size (width and height).
    private const int EXPECTED_TABLE_SIZE = 920;

    public const string OverlayStoryboardResourceName = "StbHideOverlay";
    public const string CallActionButtonStyleResourceName = "StyleCallActionButton";

    private readonly GameViewModel _viewModel;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="setup">How the game is set up.</param>
    /// <param name="dialogs">Opens the secondary windows (rules, statistics, score...).</param>
    /// <param name="settings">The user's settings.</param>
    /// <param name="storage">Where the player statistics are kept.</param>
    public MainWindow(HumanGameSetup setup, IDialogService dialogs, IUserSettings settings, IPlayerStatisticsStorage storage)
    {
        InitializeComponent();

        var animations = new WpfAnimationService(GrdOverlayCall, BtnOpponentCall, (Storyboard)FindResource(OverlayStoryboardResourceName));
        _viewModel = new GameViewModel(setup, dialogs, settings, storage, new WpfUiDispatcher(), new TaskDelay(), animations, new WpfSoundService());
        _viewModel.CloseRequested += (sender, e) => Close();
        DataContext = _viewModel;

        FixWindowDimensions();

        BindConfiguration();

        ContentRendered += (sender, e) => _viewModel.StartCommand.Execute(null);
    }

    #region Window events

    private void Window_Closing(object sender, CancelEventArgs e)
    {
        _viewModel.Cancel();
    }

    #region Configuration

    private void CbbCpuSpeed_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (IsLoaded && CbbCpuSpeed.SelectedIndex >= 0)
        {
            Properties.Settings.Default.CpuSpeed = CbbCpuSpeed.SelectedIndex;
            Properties.Settings.Default.Save();
        }
    }

    private void CbbChrono_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (IsLoaded && CbbChrono.SelectedIndex >= 0)
        {
            Properties.Settings.Default.ChronoSpeed = CbbChrono.SelectedIndex;
            Properties.Settings.Default.Save();
        }
    }

    private void ChkSounds_Click(object sender, RoutedEventArgs e)
    {
        Properties.Settings.Default.PlaySounds = ChkSounds.IsChecked == true;
        Properties.Settings.Default.Save();
    }

    private void ChkAutoTsumoRon_Click(object sender, RoutedEventArgs e)
    {
        Properties.Settings.Default.AutoCallMahjong = ChkAutoTsumoRon.IsChecked == true;
        Properties.Settings.Default.Save();
    }

    #endregion Configuration

    #endregion Window events

    // Fix dimensions of the window and every panels (when it's required).
    private void FixWindowDimensions()
    {
        Title = WINDOW_TITLE;

        GrdMain.Width = EXPECTED_TABLE_SIZE;
        GrdMain.Height = EXPECTED_TABLE_SIZE;
        Height = EXPECTED_TABLE_SIZE + 50; // Ugly !

        double dim1 = TileButton.TILE_HEIGHT + TileButton.DEFAULT_TILE_MARGIN;
        double dim2 = (TileButton.TILE_HEIGHT * 3) + (TileButton.DEFAULT_TILE_MARGIN * 2);
        var dim3 = EXPECTED_TABLE_SIZE - ((dim1 * 4) + (dim2 * 2));

        Cod0.Width = new GridLength(dim1);
        Cod1.Width = new GridLength(dim1);
        Cod2.Width = new GridLength(dim2);
        Cod3.Width = new GridLength(dim3);
        Cod4.Width = new GridLength(dim2);
        Cod5.Width = new GridLength(dim1);
        Cod6.Width = new GridLength(dim1);

        Rod0.Height = new GridLength(dim1);
        Rod1.Height = new GridLength(dim1);
        Rod2.Height = new GridLength(dim2);
        Rod3.Height = new GridLength(dim3);
        Rod4.Height = new GridLength(dim2);
        Rod5.Height = new GridLength(dim1);
        Rod6.Height = new GridLength(dim1);
    }

    // Binds graphic elements with current configuration.
    private void BindConfiguration()
    {
        CbbChrono.ItemsSource = DisplayTexts.GetChronoDisplayValues();
        CbbChrono.SelectedIndex = Properties.Settings.Default.ChronoSpeed;

        CbbCpuSpeed.ItemsSource = DisplayTexts.GetCpuSpeedDisplayValues();
        CbbCpuSpeed.SelectedIndex = Properties.Settings.Default.CpuSpeed;

        ChkSounds.IsChecked = Properties.Settings.Default.PlaySounds;
        ChkAutoTsumoRon.IsChecked = Properties.Settings.Default.AutoCallMahjong;
    }
}
