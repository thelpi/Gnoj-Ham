using System.Windows;
using Gnoj_Ham_ViewModel.Services;

namespace Gnoj_Ham_View.Services;

/// <summary>
/// Runs work on the application's UI thread.
/// </summary>
internal sealed class WpfUiDispatcher : IUiDispatcher
{
    /// <inheritdoc />
    public void Invoke(Action action) => Application.Current.Dispatcher.Invoke(action);

    /// <inheritdoc />
    public Task InvokeAsync(Action action) => Application.Current.Dispatcher.InvokeAsync(action).Task;
}
