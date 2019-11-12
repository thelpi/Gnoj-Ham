using System;
using System.Diagnostics;
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
#if DEBUG
            AppDomain.CurrentDomain.FirstChanceException += (innerSender, innerE) =>
            {
                Debug.WriteLine($"{innerE.Exception.Message} - {innerE.Exception.StackTrace}");
            };
#endif

            new IntroWindow().ShowDialog();
        }
    }
}
