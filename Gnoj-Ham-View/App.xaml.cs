using System.IO;
using System.Windows;
using System.Windows.Controls;
using Gnoj_Ham_View.Services;
using Gnoj_Ham_ViewModel;
using Gnoj_Ham_ViewModel.Services;

namespace Gnoj_Ham_View;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private const double MinimalHeightResolution = 1024;

    private static readonly string SettingsFilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Gnoj-Ham", "settings.json");

    static App()
    {
        ToolTipService.InitialShowDelayProperty.OverrideMetadata(
            typeof(FrameworkElement),
            new FrameworkPropertyMetadata(50));
    }

    private void Application_Startup(object sender, StartupEventArgs e)
    {
        var screenHeight = SystemParameters.PrimaryScreenHeight;
        if (screenHeight < MinimalHeightResolution)
        {
            var response = MessageBox.Show($"Votre résolution d'écran est trop basse ; une hauteur de {MinimalHeightResolution}px ou plus est fortement recommandée. Continuer ?", "Gnoj-Ham - Avertissement", MessageBoxButton.YesNo);
            if (response == MessageBoxResult.No)
            {
                Environment.Exit(0);
            }
        }

        try
        {
            // The settings are shared by every window: what one changes, the others see.
            var settingsStorage = new JsonUserSettingsStorage(SettingsFilePath);
            var settings = settingsStorage.Load();

            var dialogs = CreateDialogService(settings, settingsStorage);
            dialogs.ShowDialog(new IntroViewModel(settings, settingsStorage, new FilePlayerStatisticsStorage(), dialogs, new WpfUiDispatcher()));
        }
        catch (Exception ex)
        {
            var localEx = ex;
            while (localEx.InnerException != null)
            {
                localEx = localEx.InnerException;
            }
            Clipboard.SetText($"{localEx.Message}\r\n\r\n{localEx.StackTrace}");
            MessageBox.Show("Une erreur technique est survenue, entrainant l'arrêt de l'application.\r\nLes détails de l'erreur ont été copiées dans le presse-papier.\r\nMerci d'avance des les transmettre à l'équipe technique.", "Gnoj-Ham - Erreur");
            Environment.Exit(0);
        }
    }

    // Says which window displays which view-model.
    private static WpfDialogService CreateDialogService(UserSettings settings, IUserSettingsStorage settingsStorage)
    {
        var dialogs = new WpfDialogService();
        dialogs.Register<IntroViewModel>(viewModel => new IntroWindow(viewModel));
        dialogs.Register<AutoPlayViewModel>(viewModel => new AutoPlayWindow(viewModel));
        dialogs.Register<HumanGameSetup>(setup => new MainWindow(setup, dialogs, settings, settingsStorage, new FilePlayerStatisticsStorage()));
        dialogs.Register<RulesViewModel>(_ => new RulesWindow());
        dialogs.Register<PlayerSaveStatsViewModel>(_ => new PlayerSaveStatsWindow());
        dialogs.Register<ScoreViewModel>(_ => new ScoreWindow());
        dialogs.Register<EndOfGameViewModel>(_ => new EndOfGameWindow());
        return dialogs;
    }
}