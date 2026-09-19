using System.Windows;
using Gnoj_Ham_ViewModel.Services;

namespace Gnoj_Ham_View.Services;

/// <summary>
/// Opens the window registered for a view-model. View-models only ever ask for "show this": which
/// window displays them is decided once, where the application is composed.
/// </summary>
internal sealed class WpfDialogService : IDialogService
{
    private readonly Dictionary<Type, Func<object, Window>> _windowFactories = new();

    /// <summary>
    /// Registers the window that displays a view-model type.
    /// </summary>
    /// <typeparam name="TViewModel">The view-model type.</typeparam>
    /// <param name="createWindow">Creates the window for a view-model instance.</param>
    internal void Register<TViewModel>(Func<TViewModel, Window> createWindow) where TViewModel : class
    {
        _windowFactories[typeof(TViewModel)] = viewModel => createWindow((TViewModel)viewModel);
    }

    /// <inheritdoc />
    public void ShowDialog<TViewModel>(TViewModel viewModel) where TViewModel : class
    {
        if (!_windowFactories.TryGetValue(viewModel.GetType(), out var createWindow))
        {
            throw new InvalidOperationException($"No window is registered for {viewModel.GetType().Name}.");
        }

        var window = createWindow(viewModel);

        // A window that has already picked its own data context (e.g. a table view-model built from
        // the game it is given) keeps it.
        window.DataContext ??= viewModel;
        window.ShowDialog();
    }

    /// <inheritdoc />
    public void ShowMessage(string message, string title)
    {
        MessageBox.Show(message, title);
    }
}
