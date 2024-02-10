using System;
using System.Windows;

namespace Gnoj_HamView
{
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

            new IntroWindow().ShowDialog();
        }
    }
}
