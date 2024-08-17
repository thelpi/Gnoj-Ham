using Gnoj_Ham_Library.Enums;

namespace Gnoj_Ham_Library.Abstractions;

/// <summary>
/// Interface for CPU decisions.
/// </summary>
public interface ICpuDecisionsManagerPivot
{
    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    TilePivot DiscardDecision();

    /// <summary>
    /// Checks if the current player can call 'Riichi' and computes the decision to do so.
    /// </summary>
    /// <returns>The tile to discard if 'Riichi' is called; <c>Null</c> otherwise.</returns>
    TilePivot? RiichiDecision();

    /// <summary>
    /// Checks if the specified player can call 'Ron' and computes the decision to do so.
    /// </summary>
    /// <param name="playerIndex">Player index.</param>
    /// <param name="humanRonCalled">Indicates if the human player has also made a 'Ron' call.</param>
    /// <returns><c>True</c> if 'Ron' called; <c>False</c> otherwise, or if <paramref name="playerIndex"/> is human.</returns>
    bool RonDecision(PlayerIndices playerIndex, bool humanRonCalled);

    /// <summary>
    /// Computes an advice for the human player to call a Kan or not; assumes the Kan is possible.
    /// </summary>
    /// <param name="kanPossibilities">The first tile of every possible Kan at the moment.</param>
    /// <param name="concealedKan"><c>True</c> if the context is a concealed Kan.</param>
    /// <returns><c>True</c> if Kan is advised.</returns>
    bool KanDecisionAdvice(PlayerIndices pIndex, IReadOnlyList<TilePivot> kanPossibilities, bool concealedKan);

    /// <summary>
    /// Computes an advice for the human player to call a Pon or not; assumes the Pon is possible.
    /// </summary>
    /// <returns><c>True</c> if Pon is advised.</returns>
    bool PonDecisionAdvice(PlayerIndices pIndex);

    /// <summary>
    /// Computes an advice for the human player to call a Chii or not; assumes the Chii is possible.
    /// </summary>
    /// <param name="chiiPossibilities">See the result of the method <see cref="RoundPivot.CanCallChii"/>.</param>
    /// <returns><c>True</c> if Chii is advised.</returns>
    bool ChiiDecisionAdvice(IReadOnlyList<TilePivot> chiiPossibilities);

    /// <summary>
    /// Checks if any CPU player can make a pon call, and computes its decision if any.
    /// </summary>
    /// <returns>The player index who makes the call; <c>-1</c> is none.</returns>
    PlayerIndices? PonDecision();

    /// <summary>
    /// Checks if the current CPU player can make a tsumo call, and computes the decision to do so.
    /// </summary>
    /// <param name="isKanCompensation"><c>True</c> if it's while a kan call is in progress; <c>False</c> otherwise.</param>
    /// <returns><c>True</c> if the decision is made; <c>False</c> otherwise.</returns>
    bool TsumoDecision(bool isKanCompensation);

    /// <summary>
    /// Checks for CPU players who can make a kan call, and computes the decision to call it.
    /// </summary>
    /// <param name="checkConcealedOnly">
    /// <c>True</c> to check only concealed kan (or from a previous pon);
    /// <c>False</c> to check the opposite.
    /// </param>
    /// <returns>
    /// If the decision is made, a tuple :
    /// - The first item indicates the player index who call the kan.
    /// - The second item indicates the base tile of the kand (several choices are possible).
    /// <c>Null</c> otherwise.
    /// </returns>
    (PlayerIndices pIndex, TilePivot tile)? KanDecision(bool checkConcealedOnly);

    /// <summary>
    /// Checks if the current CPU player can make a chii call, and computes the decision to do so.
    /// </summary>
    /// <returns>
    /// If the decision is made, the first tile to use, in the sequence order, in the concealed hand of the player.
    /// <c>Null</c> otherwise.
    /// </returns>
    TilePivot? ChiiDecision();
}
