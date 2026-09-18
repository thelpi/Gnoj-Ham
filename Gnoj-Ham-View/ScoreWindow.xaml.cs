using System.Windows;

namespace Gnoj_Ham_View;

/// <summary>
/// Interaction logic for ScoreWindow.xaml.
/// </summary>
public partial class ScoreWindow : Window
{
    /// <summary>
    /// Constructor.
    /// </summary>
    public ScoreWindow()
    {
        InitializeComponent();
    }

    private void BtnGoToNext_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
