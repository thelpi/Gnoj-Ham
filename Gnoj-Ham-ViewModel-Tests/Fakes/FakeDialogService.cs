using Gnoj_Ham_ViewModel.Services;

namespace Gnoj_Ham_ViewModel_Tests.Fakes;

/// <summary>
/// Opens nothing; records the view-models a view-model asked to display, and the messages it showed.
/// </summary>
internal sealed class FakeDialogService : IDialogService
{
    private readonly List<object> _shownViewModels = new();
    private readonly List<(string message, string title)> _shownMessages = new();

    public IReadOnlyList<object> ShownViewModels => _shownViewModels;

    public IReadOnlyList<(string message, string title)> ShownMessages => _shownMessages;

    /// <summary>
    /// Runs while a dialog is "open", like whatever the user does inside a real modal window.
    /// </summary>
    public Action<object>? OnShowDialog { get; set; }

    public void ShowDialog<TViewModel>(TViewModel viewModel) where TViewModel : class
    {
        _shownViewModels.Add(viewModel);
        OnShowDialog?.Invoke(viewModel);
    }

    public void ShowMessage(string message, string title)
        => _shownMessages.Add((message, title));
}
