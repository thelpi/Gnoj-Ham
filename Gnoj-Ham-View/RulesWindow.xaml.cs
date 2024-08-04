using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using Gnoj_Ham_Library;

namespace Gnoj_Ham_View;

/// <summary>
/// Logique d'interaction pour YakusWindow.xaml
/// </summary>
public partial class RulesWindow : Window
{
    /// <summary>
    /// Ctor
    /// </summary>
    public RulesWindow()
    {
        InitializeComponent();

        LstYakus.ItemsSource = YakuPivot.Yakus
            .Except([YakuPivot.NagashiMangan])
            .OrderBy(x => x.ConcealedFanCount)
            .ThenBy(x => x.FanCount)
            .ThenBy(x => x.Name);
    }

    private void ScrollViewer_PreviewMouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
    {
        var scv = (ScrollViewer)sender;
        scv.ScrollToVerticalOffset(scv.VerticalOffset - (e.Delta / 7));
        e.Handled = true;
    }

    private void Hyperlink_Click(object sender, RoutedEventArgs e)
    {
        // never called, the related WPF content is disabled...
        var hlk = sender as Hyperlink;
        Process.Start("explorer", hlk!.NavigateUri.ToString());
    }
}
