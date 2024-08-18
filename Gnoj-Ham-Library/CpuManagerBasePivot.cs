using Gnoj_Ham_Library.Enums;

namespace Gnoj_Ham_Library;

/// <summary>
/// Interface for CPU decisions.
/// </summary>
public abstract class CpuManagerBasePivot
{
    protected static readonly int[] MiddleNumbers = new[] { 4, 5, 6 };

    /// <summary>
    /// Instnace of <see cref="RoundPivot"/>.
    /// </summary>
    protected RoundPivot Round { get; }

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="round"><see cref="Round"/></param>
    protected CpuManagerBasePivot(RoundPivot round)
    {
        Round = round;
    }

    /// <summary>
    /// Computes the best tile to discard for the current player.
    /// </summary>
    /// <returns>The tile to discard.</returns>
    public TilePivot DiscardDecision()
    {
        var concealedTiles = Round.GetHand(Round.CurrentPlayerIndex).ConcealedTiles;

        var discardableTiles = concealedTiles
            .Where(Round.CanDiscard)
            .Distinct()
            .ToList();

        return discardableTiles.Count == 1
            ? discardableTiles[0]
            : DiscardDecisionInternal(concealedTiles, discardableTiles);
    }

    /// <summary>
    /// Checks if the current player can call 'Riichi' and computes the decision to do so.
    /// </summary>
    /// <returns>The tile to discard if 'Riichi' is called; <c>Null</c> otherwise.</returns>
    public TilePivot? RiichiDecision()
    {
        var riichiTiles = Round.CanCallRiichi();

        return riichiTiles.Count == 0
            ? null
            : RiichiDecisionInternal(riichiTiles);
    }

    /// <summary>
    /// Checks if the specified player can call 'Ron' and computes the decision to do so. 
    /// </summary>
    /// <param name="playerIndex">Player index.</param>
    /// <param name="otherCallRon">Indicates if other players have already call 'Ron'.</param>
    /// <returns><c>True</c> if calling 'Ron'.</returns>
    public bool RonDecision(PlayerIndices playerIndex, bool otherCallRon)
    {
        return Round.CanCallRon(playerIndex)
            && RonDecisionInternal(playerIndex, otherCallRon);
    }

    /// <summary>
    /// Checks if the specified player can call 'Ron' and computes the decision to do so.
    /// </summary>
    /// <returns>The player index if 'Pon' called; <c>Null</c> otherwise.</returns>
    public bool PonDecision(PlayerIndices playerIndex)
    {
        return Round.CanCallPon(playerIndex)
            && PonDecisionInternal(playerIndex);
    }

    /// <summary>
    /// Checks if the current player can call 'Tsumo' and computes the decision to do so.
    /// </summary>
    /// <param name="isKanCompensation"><c>True</c> if it's while a 'Kan' call is in progress; <c>False</c> otherwise.</param>
    /// <returns><c>True</c> if 'Tsumo' is called; <c>False</c> otherwise.</returns>
    public bool TsumoDecision(bool isKanCompensation)
    {
        return Round.CanCallTsumo(isKanCompensation)
            && TsumoDecisionInternal(isKanCompensation);
    }

    /// <summary>
    /// Checks if the current player can call 'Chii' and computes the decision to do so.
    /// </summary>
    /// <returns>A tuple that indicates if chii is possible and, if that's the case, the first tile to use, in the sequence order, in the concealed hand of the player.</returns>
    public (bool canChii, TilePivot? chiiChoice) ChiiDecision()
    {
        var chiiTiles = Round.CanCallChii();
        return chiiTiles.Count == 0
            ? (false, null)
            : (true, ChiiDecisionInternal(chiiTiles));
    }

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
    public  (PlayerIndices pIndex, TilePivot tile)? KanDecision(bool checkConcealedOnly)
    {
        (PlayerIndices pIndex, TilePivot tile)? callData = null;
        foreach (var i in Enum.GetValues<PlayerIndices>())
        {
            if (Round.Game.IsCpu(i))
            {
                var kanTiles = Round.CanCallKanWithChoices(i, checkConcealedOnly);
                if (kanTiles.Count > 0)
                {
                    callData = KanDecisionInternal(i, kanTiles, checkConcealedOnly);
                    break;
                }
            }
        }

        return callData;
    }

    /// <summary>
    /// Computes an advice for the human player to call a Kan or not; assumes the Kan is possible.
    /// </summary>
    /// <param name="kanPossibilities">The first tile of every possible Kan at the moment.</param>
    /// <param name="concealedKan"><c>True</c> if the context is a concealed Kan.</param>
    /// <returns><c>True</c> if Kan is advised.</returns>
    public bool KanDecisionAdvice(PlayerIndices pIndex, IReadOnlyList<TilePivot> kanPossibilities, bool concealedKan)
        => KanDecisionInternal(pIndex, kanPossibilities, concealedKan).HasValue;

    #region Protected logic to override

    protected abstract TilePivot DiscardDecisionInternal(IReadOnlyList<TilePivot> concealedTiles, List<TilePivot> discardableTiles);

    protected abstract TilePivot RiichiDecisionInternal(IReadOnlyList<TilePivot> riichiTiles);

    protected virtual bool RonDecisionInternal(PlayerIndices playerIndex, bool otherCallRon)
    {
        return true;
    }

    protected abstract bool PonDecisionInternal(PlayerIndices playerIndex);

    protected virtual bool TsumoDecisionInternal(bool isKanCompensation)
    {
        return true;
    }

    protected abstract (PlayerIndices playerIndex, TilePivot tile)? KanDecisionInternal(PlayerIndices playerIndex, IReadOnlyList<TilePivot> kanPossibilities, bool concealed);

    protected abstract TilePivot? ChiiDecisionInternal(IReadOnlyList<TilePivot> chiiTiles);

    #endregion Protected logic to override

    #region Common protected logic

    /// <summary>
    /// Checks if a tile is safe to discard, relative to the specified opponent.
    /// </summary>
    /// <param name="tile">The tile to check.</param>
    /// <param name="opponentPlayerIndex">Opponent player index.</param>
    /// <param name="povVisibleTiles">Tiles that are visible from the current player POV (dead tiles + tiles in hand).</param>
    /// <returns><c>True</c> if the tile is guaranteed safe for discard (relative to opponent); <c>False</c> otherwise.</returns>
    protected bool IsGuaranteedSafe(TilePivot tile, PlayerIndices opponentPlayerIndex, IReadOnlyList<TilePivot> povVisibleTiles)
    {
        return Round.GetDiscard(opponentPlayerIndex).Contains(tile) || (
            tile.IsHonor
            // honor that can't be paired by opponent
            && povVisibleTiles.Count(t => t == tile) == 4
            // at least one other honor (or terminal) is fully inaccessible for opponent to make Kokushi-Musuou
            && povVisibleTiles.GroupBy(t => t).Any(t => t.Key != tile && t.Key.IsHonorOrTerminal && t.Count() > 3)
        );
    }

    /// <summary>
    /// Checks if a tile is suji on edge from the discard of an opponent.
    /// </summary>
    /// <param name="tile">The tile to check.</param>
    /// <param name="opponentPlayerIndex">Opponent player index.</param>
    /// <returns><c>True</c> if suji; <c>False</c> otherwise.</returns>
    protected bool IsInsiderSuji(TilePivot tile, PlayerIndices opponentPlayerIndex)
    {
        return GetSujisFromDiscard(tile, opponentPlayerIndex)
            .Any(_ => !MiddleNumbers.Contains(_.Number));
    }

    /// <summary>
    /// Checks if a tile is suji on middle from the discard of an opponent.
    /// </summary>
    /// <param name="tile">The tile to check.</param>
    /// <param name="opponentPlayerIndex">Opponent player index.</param>
    /// <returns><c>True</c> if suji; <c>False</c> otherwise.</returns>
    protected bool IsOutsiderSuji(TilePivot tile, PlayerIndices opponentPlayerIndex)
    {
        return GetSujisFromDiscard(tile, opponentPlayerIndex)
            .Any(_ => MiddleNumbers.Contains(_.Number));
    }

    /// <summary>
    /// Checks if a tile is suji on both sides from the discard of an opponent.
    /// </summary>
    /// <param name="tile">The tile to check.</param>
    /// <param name="opponentPlayerIndex">Opponent player index.</param>
    /// <returns><c>True</c> if suji; <c>False</c> otherwise.</returns>
    protected bool IsDoubleInsiderSuji(TilePivot tile, PlayerIndices opponentPlayerIndex)
    {
        return GetSujisFromDiscard(tile, opponentPlayerIndex).Distinct().Count() > 1;
    }

    private IEnumerable<TilePivot> GetSujisFromDiscard(TilePivot tile, PlayerIndices opponentPlayerIndex)
    {
        return tile.IsHonor
            ? Enumerable.Empty<TilePivot>()
            : Round
                .GetDiscard(opponentPlayerIndex)
                .Where(_ => _.Family == tile.Family && (_.Number == tile.Number + 3 || _.Number == tile.Number - 3));
    }

    #endregion

    /// <summary>
    /// Enumeration of level of safety for tile discard.
    /// </summary>
    protected enum TileSafety
    {
        /// <summary>
        /// The tile is 100% safe.
        /// </summary>
        Safe,
        /// <summary>
        /// The tile seems safe.
        /// </summary>
        QuiteSafe,
        /// <summary>
        /// Unable to detect the safety level of the tile.
        /// </summary>
        AverageOrUnknown,
        /// <summary>
        /// The tile seems unsafe.
        /// </summary>
        QuiteUnsafe,
        /// <summary>
        /// The tile is unsafe.
        /// </summary>
        Unsafe
    }
}
