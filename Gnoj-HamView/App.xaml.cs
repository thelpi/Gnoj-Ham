using System;
using System.Windows;
using Gnoj_HamView.Properties;

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
            SetLanguageDictionary(Resources);

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

        internal static void SetLanguageDictionary(ResourceDictionary resources)
        {
            var dict = new ResourceDictionary();
            switch ((Languages)Settings.Default.Language)
            {
                case Languages.fr:
                    dict.Source = new Uri("..\\Resources\\StringResources_fr.xaml", UriKind.Relative);
                    break;
                default:
                    dict.Source = new Uri("..\\Resources\\StringResources_en.xaml", UriKind.Relative);
                    break;
            }
            resources.MergedDictionaries.Add(dict);
        }
    }
}
