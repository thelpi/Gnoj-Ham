using Gnoj_Ham_Library;
using Gnoj_Ham_Library.Enums;

namespace Gnoj_Ham_ViewModel;

/// <summary>
/// What the human player's choices lead to: whoever runs the game carries them out and plays on until
/// the human player is needed again.
/// </summary>
public interface IHumanActions
{
    /// <summary>
    /// Discards a tile of the human player's hand.
    /// </summary>
    /// <param name="tile">The tile.</param>
    /// <returns>A task completing once the game needs the human player again.</returns>
    Task DiscardAsync(TilePivot tile);

    /// <summary>
    /// Makes a call for the human player.
    /// </summary>
    /// <param name="call">The call.</param>
    /// <returns>A task completing once the game needs the human player again.</returns>
    Task CallAsync(CallTypes call);

    /// <summary>
    /// Turns down the calls offered to the human player - or, when none is, discards the tile just picked.
    /// </summary>
    /// <returns>A task completing once the game needs the human player again.</returns>
    Task SkipCallsAsync();
}
