using Gnoj_Ham_Library.Enums;
using Gnoj_Ham_ViewModel.Services;

namespace Gnoj_Ham_ViewModel_Tests.Fakes;

/// <summary>
/// Plays nothing and finishes immediately - unless told to hold the announcements until released;
/// records each announced call.
/// </summary>
internal sealed class FakeAnimationService : IAnimationService
{
    private readonly List<(CallTypes call, PlayerIndices playerIndex)> _announcedCalls = new();
    private readonly List<TaskCompletionSource> _held = new();

    public IReadOnlyList<(CallTypes call, PlayerIndices playerIndex)> AnnouncedCalls => _announcedCalls;

    /// <summary>
    /// Makes the announcements last until <see cref="Release"/>.
    /// </summary>
    public bool Hold { get; set; }

    public Task PlayCallAnnouncementAsync(CallTypes call, PlayerIndices playerIndex)
    {
        lock (_announcedCalls)
        {
            _announcedCalls.Add((call, playerIndex));
        }

        if (!Hold)
        {
            return Task.CompletedTask;
        }

        var source = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        lock (_held)
        {
            _held.Add(source);
        }
        return source.Task;
    }

    /// <summary>
    /// Ends the announcements held.
    /// </summary>
    public void Release()
    {
        List<TaskCompletionSource> held;
        lock (_held)
        {
            held = _held.ToList();
            _held.Clear();
        }

        foreach (var source in held)
        {
            source.TrySetResult();
        }
    }
}
