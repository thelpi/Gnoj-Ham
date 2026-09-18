using System.Windows;
using System.Windows.Controls;
using Gnoj_Ham_ViewModel;

namespace Gnoj_Ham_View;

/// <summary>
/// Interaction logic for IntroWindow.xaml
/// </summary>
public partial class IntroWindow : Window
{
    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="viewModel">The window's view-model.</param>
    public IntroWindow(IntroViewModel viewModel)
    {
        InitializeComponent();

        DataContext = viewModel;

        // The window steps aside while a game is played, then comes back.
        viewModel.HideRequested += (sender, e) => Hide();
        viewModel.ShowRequested += (sender, e) => ShowDialog();

        DevelopmentTab.Visibility = Visibility.Visible;

        // A TabControl only ever measures its currently-selected tab's content, so SizeToContent
        // alone would make the window resize itself on every tab switch. Measuring every tab's own
        // content up front and taking the tallest one settles the window at that height regardless
        // of which tab ends up selected - no more manual Height resync when a tab's content grows.
        MainTabControl.MinHeight = MainTabControl.Items
            .OfType<TabItem>()
            .Max(tab =>
            {
                var content = (FrameworkElement)tab.Content;
                content.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                return content.DesiredSize.Height;
            });
    }

    private void BtnQuit_Click(object sender, RoutedEventArgs e)
    {
        Environment.Exit(0);
    }
}
