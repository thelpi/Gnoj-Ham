using System.Windows;

namespace Gnoj_HamView
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            new IntroWindow().ShowDialog();
        }
    }
}
