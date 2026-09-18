using Gnoj_Ham_ViewModel.Services;

namespace Gnoj_Ham_ViewModel_Tests.Fakes;

/// <summary>
/// Opens nothing; records the view-models a view-model asked to display.
/// </summary>
internal sealed class FakeDialogService : IDialogService
{
    private readonly List<object> _shownViewModels = new();

    public IReadOnlyList<object> ShownViewModels => _shownViewModels;

    public void ShowDialog<TViewModel>(TViewModel viewModel) where TViewModel : class
        => _shownViewModels.Add(viewModel);
}
