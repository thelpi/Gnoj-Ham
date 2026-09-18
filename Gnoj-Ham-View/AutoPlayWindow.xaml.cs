using System.ComponentModel;
using System.Windows;
using Gnoj_Ham_ViewModel;

namespace Gnoj_Ham_View;

/// <summary>
/// Interaction logic for AutoPlayWindow.xaml
/// </summary>
public partial class AutoPlayWindow : Window
{
    private readonly AutoPlayViewModel _viewModel;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="viewModel">The window's view-model.</param>
    public AutoPlayWindow(AutoPlayViewModel viewModel)
    {
        InitializeComponent();

        _viewModel = viewModel;
        DataContext = viewModel;

        viewModel.BatchCompleted += (sender, e) => WindowState = WindowState.Maximized;
    }

    private void Window_Closing(object sender, CancelEventArgs e)
    {
        _viewModel.Cancel();
    }
}
