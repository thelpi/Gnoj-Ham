using System.ComponentModel;
using System.Windows;
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
    public const string OverlayStoryboardResourceName = "StbHideOverlay";
    public const string CallActionButtonStyleResourceName = "StyleCallActionButton";

    private readonly GameViewModel _viewModel;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="setup">How the game is set up.</param>
    /// <param name="dialogs">Opens the secondary windows (rules, statistics, score...).</param>
    /// <param name="settings">The user's settings.</param>
    /// <param name="settingsStorage">Where the user's settings are kept.</param>
    /// <param name="storage">Where the player statistics are kept.</param>
    public MainWindow(HumanGameSetup setup, IDialogService dialogs, UserSettings settings, IUserSettingsStorage settingsStorage, IPlayerStatisticsStorage storage)
    {
        InitializeComponent();

        var animations = new WpfAnimationService(GrdOverlayCall, BtnOpponentCall, (Storyboard)FindResource(OverlayStoryboardResourceName));
        _viewModel = new GameViewModel(setup, dialogs, settings, settingsStorage, storage, new WpfUiDispatcher(), new TaskDelay(), animations, new WpfSoundService());
        _viewModel.CloseRequested += (sender, e) => Close();
        DataContext = _viewModel;

        ContentRendered += (sender, e) => _viewModel.StartCommand.Execute(null);
    }

    private void Window_Closing(object sender, CancelEventArgs e)
    {
        _viewModel.Cancel();
    }
}
