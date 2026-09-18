namespace Gnoj_Ham_ViewModel.Services;

/// <summary>
/// Opens secondary windows from a view-model, without it knowing any window type: the view layer maps
/// each view-model type to the window that displays it.
/// </summary>
public interface IDialogService
{
    /// <summary>
    /// Shows the window associated with <typeparamref name="TViewModel"/> modally, with
    /// <paramref name="viewModel"/> as its data context, and returns once it is closed.
    /// </summary>
    /// <typeparam name="TViewModel">The view-model type.</typeparam>
    /// <param name="viewModel">The view-model to display.</param>
    void ShowDialog<TViewModel>(TViewModel viewModel) where TViewModel : class;
}
