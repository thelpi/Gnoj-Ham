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
                var response = MessageBox.Show($"Your screen resolution is too low; an height of {MinimalHeightResolution}px or more is highly recommanded. Continue?", "Gnoj-Ham - Warning", MessageBoxButton.YesNo);
                if (response == MessageBoxResult.No)
                {
                    Environment.Exit(0);
                }
            }

            new IntroWindow().ShowDialog();
        }
    }
}
