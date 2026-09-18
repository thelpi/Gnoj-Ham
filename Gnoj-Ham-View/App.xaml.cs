using System.Windows;
using System.Windows.Controls;
using Gnoj_Ham_View.Services;
using Gnoj_Ham_ViewModel;

namespace Gnoj_Ham_View;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private const double MinimalHeightResolution = 1024;

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
            new IntroWindow(CreateDialogService()).ShowDialog();
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
    private static WpfDialogService CreateDialogService()
    {
        var dialogs = new WpfDialogService();
        dialogs.Register<RulesViewModel>(_ => new RulesWindow());
        dialogs.Register<PlayerSaveStatsViewModel>(_ => new PlayerSaveStatsWindow());
        dialogs.Register<ScoreViewModel>(_ => new ScoreWindow());
        dialogs.Register<EndOfGameViewModel>(_ => new EndOfGameWindow());
        return dialogs;
    }
}