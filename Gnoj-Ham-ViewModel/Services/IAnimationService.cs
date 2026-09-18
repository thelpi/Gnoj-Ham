using Gnoj_Ham_Library.Enums;

namespace Gnoj_Ham_ViewModel.Services;

/// <summary>
/// Plays the game's animations and lets the game flow await their end, instead of the flow being
/// driven by animation-completed callbacks.
/// </summary>
public interface IAnimationService
{
    /// <summary>
    /// Shows the announcement of a call (pon, chii, kan, ron, riichi...) next to the seat that made it.
    /// </summary>
    /// <param name="call">The call to announce.</param>
    /// <param name="playerIndex">The player who made the call.</param>
    /// <returns>A task completing once the announcement has fully finished playing.</returns>
    Task PlayCallAnnouncementAsync(CallTypes call, PlayerIndices playerIndex);
}
