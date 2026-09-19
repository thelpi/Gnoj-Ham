using Gnoj_Ham_Library;
using Gnoj_Ham_Library.Enums;
using Gnoj_Ham_ViewModel;

namespace Gnoj_Ham_ViewModel_Tests.Fakes;

/// <summary>
/// Does nothing but record what the human player chose.
/// </summary>
internal sealed class FakeHumanActions : IHumanActions
{
    public List<TilePivot> Discarded { get; } = new();

    public List<CallTypes> Calls { get; } = new();

    public int SkipCount { get; private set; }

    public Task DiscardAsync(TilePivot tile)
    {
        Discarded.Add(tile);
        return Task.CompletedTask;
    }

    public Task CallAsync(CallTypes call)
    {
        Calls.Add(call);
        return Task.CompletedTask;
    }

    public Task SkipCallsAsync()
    {
        SkipCount++;
        return Task.CompletedTask;
    }
}
