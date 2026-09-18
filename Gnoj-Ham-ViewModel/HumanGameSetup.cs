using Gnoj_Ham_Library;

namespace Gnoj_Ham_ViewModel;

/// <summary>
/// Everything needed to start a game with a human player. Shown through
/// <see cref="Services.IDialogService"/> like any view-model, until the game table gets a view-model of
/// its own to be shown instead.
/// </summary>
/// <param name="PlayerName">The human player name.</param>
/// <param name="Ruleset">The ruleset.</param>
/// <param name="Stats">The player statistics.</param>
/// <param name="DrivenDraw">Optional; forces a specific starting hand for the human player (see <see cref="DrivenDrawPivot"/>). <c>Null</c> for a normal, fully random draw.</param>
/// <param name="DebugMode">Reveals every hand instead of just the human player's.</param>
public sealed record HumanGameSetup(
    string PlayerName,
    RulePivot Ruleset,
    PlayerStatisticsPivot Stats,
    Action<List<TilePivot>>? DrivenDraw,
    bool DebugMode);
