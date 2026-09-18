using Gnoj_Ham_Library.Enums;
using Gnoj_Ham_ViewModel.Services;

namespace Gnoj_Ham_ViewModel_Tests.Fakes;

/// <summary>
/// Plays nothing and finishes immediately; records each announced call.
/// </summary>
internal sealed class FakeAnimationService : IAnimationService
{
    private readonly List<(CallTypes call, PlayerIndices playerIndex)> _announcedCalls = new();

    public IReadOnlyList<(CallTypes call, PlayerIndices playerIndex)> AnnouncedCalls => _announcedCalls;

    public Task PlayCallAnnouncementAsync(CallTypes call, PlayerIndices playerIndex)
    {
        _announcedCalls.Add((call, playerIndex));
        return Task.CompletedTask;
    }
}
