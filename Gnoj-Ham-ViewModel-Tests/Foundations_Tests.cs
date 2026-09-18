using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Gnoj_Ham_ViewModel_Tests.Fakes;

namespace Gnoj_Ham_ViewModel_Tests;

// "partial" because the toolkit's generators emit code for the nested view-model below, which
// requires every containing type to be partial too.
public partial class Foundations_Tests
{
    // Throwaway view-model exercising the toolkit's source generators (ObservableProperty and
    // RelayCommand): proves they compile and behave under this solution's warnings-as-errors setup.
    private sealed partial class SampleViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _name = string.Empty;

        public int SaveCount { get; private set; }

        [RelayCommand(CanExecute = nameof(CanSave))]
        private void Save() => SaveCount++;

        private bool CanSave() => Name.Length > 0;

        partial void OnNameChanged(string value) => SaveCommand.NotifyCanExecuteChanged();
    }

    [Fact]
    public void Toolkit_ObservablePropertyRaisesPropertyChanged()
    {
        var viewModel = new SampleViewModel();
        var raised = new List<string?>();
        viewModel.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

        viewModel.Name = "Lpi";

        Assert.Equal(new[] { nameof(SampleViewModel.Name) }, raised);
    }

    [Fact]
    public void Toolkit_RelayCommandCanExecuteFollowsTheViewModelState()
    {
        var viewModel = new SampleViewModel();
        Assert.False(viewModel.SaveCommand.CanExecute(null));

        viewModel.Name = "Lpi";
        Assert.True(viewModel.SaveCommand.CanExecute(null));

        viewModel.SaveCommand.Execute(null);
        Assert.Equal(1, viewModel.SaveCount);
    }

    [Fact]
    public async Task FakeDelay_RecordsTheRequestedDelayWithoutWaiting()
    {
        var delay = new FakeDelay();

        await delay.DelayAsync(TimeSpan.FromMinutes(10));

        Assert.Equal(new[] { TimeSpan.FromMinutes(10) }, delay.RequestedDelays);
    }

    [Fact]
    public async Task FakeDelay_HonorsCancellation()
    {
        var delay = new FakeDelay();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(() => delay.DelayAsync(TimeSpan.FromSeconds(1), cts.Token));
        Assert.Empty(delay.RequestedDelays);
    }

    [Fact]
    public void FakeUiDispatcher_RunsTheActionInline()
    {
        var dispatcher = new FakeUiDispatcher();
        var ran = false;

        dispatcher.Invoke(() => ran = true);

        Assert.True(ran);
    }
}
