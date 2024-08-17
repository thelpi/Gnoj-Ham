using Gnoj_Ham_Library.Enums;

namespace Gnoj_Ham_Library;

/// <summary>
/// Interface for CPU decisions.
/// </summary>
public interface ICpuDecisionsManagerPivot
{
    /// <summary>
    /// Computes the best tile to discard for the current player.
    /// </summary>
    /// <returns>The tile to discard.</returns>
    /// <remarks>Assumes it's called in the proper context. Suitable for advice.</remarks>
    TilePivot DiscardDecision();

    /// <summary>
    /// Checks if the current player can call 'Riichi' and computes the decision to do so.
    /// </summary>
    /// <returns>The tile to discard if 'Riichi' is called; <c>Null</c> otherwise.</returns>
    /// <remarks>Suitable for advice.</remarks>
    TilePivot? RiichiDecision();

    /// <summary>
    /// Checks if any CPU player can call 'Ron' and computes the decision to do so.
    /// </summary>
    /// <param name="humanRonCalled">Indicates if the human player has also made a 'Ron' call.</param>
    /// <returns>List of player index calling 'Ron'.</returns>
    /// <remarks>Not suitable for advice.</remarks>
    IReadOnlyList<PlayerIndices> RonDecision(bool humanRonCalled);

    /// <summary>
    /// Checks if any CPU player can call 'Ron' and computes the decision to do so.
    /// </summary>
    /// <returns>The player index if 'Pon' called; <c>Null</c> otherwise.</returns>
    /// <remarks>Not suitable for advice.</remarks>
    PlayerIndices? PonDecision();

    /// <summary>
    /// Checks if the current player can call 'Tsumo' and computes the decision to do so.
    /// </summary>
    /// <param name="isKanCompensation"><c>True</c> if it's while a 'Kan' call is in progress; <c>False</c> otherwise.</param>
    /// <returns><c>True</c> if 'Tsumo' is called; <c>False</c> otherwise.</returns>
    /// <remarks>Suitable for advice.</remarks>
    bool TsumoDecision(bool isKanCompensation);

    /// <summary>
    /// Checks if the current player can call 'Chii' and computes the decision to do so.
    /// </summary>
    /// <returns>
    /// If the decision is made, the first tile to use, in the sequence order, in the concealed hand of the player.
    /// <c>Null</c> otherwise.
    /// </returns>
    /// <remarks>Not suitable for advice.</remarks>
    TilePivot? ChiiDecision();

    /// <summary>
    /// Checks if any CPU player can call 'Kan' and computes the decision to do so.
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
    /// <remarks>Not suitable for advice.</remarks>
    (PlayerIndices pIndex, TilePivot tile)? KanDecision(bool checkConcealedOnly);

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
}
