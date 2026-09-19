using Gnoj_Ham_Library;

namespace Gnoj_Ham_ViewModel;

/// <summary>
/// Everything needed to start a game with a human player. Shown through
/// <see cref="Services.IDialogService"/> like any view-model: the window that displays it builds the
/// <see cref="GameViewModel"/> that runs the game from it.
/// </summary>
/// <param name="PlayerName">The human player name.</param>
/// <param name="Ruleset">The ruleset.</param>
/// <param name="Stats">The player statistics.</param>
/// <param name="DrivenDraw">Optional; forces a specific starting hand for the human player (see <see cref="DrivenDrawPivot"/>). <c>Null</c> for a normal, fully random draw.</param>
/// <param name="DebugMode">Reveals every hand instead of just the human player's.</param>
/// <param name="Random">Optional; the randomizer the game is played with, to replay the same game. <c>Null</c> for a new one each time.</param>
public sealed record HumanGameSetup(
    string PlayerName,
    RulePivot Ruleset,
    PlayerStatisticsPivot Stats,
    Action<List<TilePivot>>? DrivenDraw,
    bool DebugMode,
    Random? Random = null);
