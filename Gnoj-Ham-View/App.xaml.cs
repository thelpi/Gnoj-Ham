using System.Windows;

namespace Gnoj_Ham_View;
/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private const double MinimalHeightResolution = 1024;

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
            new IntroWindow().ShowDialog();
        }
        catch (Exception ex)
        {
            Clipboard.SetText($"{ex.Message}\r\n\r\n{ex.StackTrace}");
            MessageBox.Show("Une erreur technique est survenue, entrainant l'arrêt de l'application.\r\nLes détails de l'erreur ont été copiées dans le presse-papier.\r\nMerci d'avance des les transmettre à l'équipe technique.", "Gnoj-Ham - Erreur");
            Environment.Exit(0);
        }
    }
}