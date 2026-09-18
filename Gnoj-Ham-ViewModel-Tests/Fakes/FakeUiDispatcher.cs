using Gnoj_Ham_ViewModel.Services;

namespace Gnoj_Ham_ViewModel_Tests.Fakes;

/// <summary>
/// Runs everything inline on the calling thread: tests have no UI thread to marshal onto.
/// </summary>
internal sealed class FakeUiDispatcher : IUiDispatcher
{
    public void Invoke(Action action) => action();

    public Task InvokeAsync(Action action)
    {
        action();
        return Task.CompletedTask;
    }
}
