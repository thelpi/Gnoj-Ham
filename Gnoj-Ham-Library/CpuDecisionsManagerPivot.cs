using Gnoj_Ham_Library.Abstractions;
using Gnoj_Ham_Library.Enums;

namespace Gnoj_Ham_Library;

/// <summary>
/// Basic implementation of <see cref="ICpuDecisionsManagerPivot"/>.
/// </summary>
public class CpuDecisionsManagerPivot : ICpuDecisionsManagerPivot
{
    private readonly RoundPivot _round;

    // indicates an honiisou (or chiniisou) in progress
    private Families? _itsuFamily;
    private static readonly int[] MiddleNumbers = new[] { 4, 5, 6 };

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="round">The <see cref="_round"/> value.</param>
    internal CpuDecisionsManagerPivot(RoundPivot round)
    {
        _round = round;
    }

    // TODO: public methods should verify parameters

    /// <inheritdoc />
    public TilePivot DiscardDecision(IReadOnlyList<TilePivot>? tenpaiPotentialDiscards)
    {
        var concealedTiles = _round.GetHand(_round.CurrentPlayerIndex).ConcealedTiles;

        var discardableTiles = concealedTiles
            .Where(_round.CanDiscard)
            .Distinct()
            .ToList();

        // there's no choice
        if (discardableTiles.Count == 1)
        {
            return discardableTiles[0];
        }

        var deadTiles = _round.DeadTilesFromIndexPointOfView(_round.CurrentPlayerIndex);

        var (tilesSafety, stopCurrentHand) = ComputeTilesSafety(discardableTiles, deadTiles);

        // tenpai: let's go anyway...
        tenpaiPotentialDiscards ??= _round.ExtractDiscardChoicesFromTenpai(_round.CurrentPlayerIndex);
        if (tenpaiPotentialDiscards.Count > 0)
        {
            return GetBestDiscardFromList(tenpaiPotentialDiscards, tilesSafety, stopCurrentHand);
        }

        if (stopCurrentHand)
        {
            return tilesSafety[0].tile;
        }

        var tilesGroup =
            concealedTiles
                .GroupBy(t => t)
                // keeps brelan/square
                .OrderByDescending(t =>
                {
                    var count = t.Count();
                    return count > 2 ? count : 0;
                })
                // keeps pair of valuable honor
                .ThenByDescending(t => t.Count() > 1
                    && (t.Key.Family == Families.Dragon
                        || t.Key.Wind == _round.Game.DominantWind
                        || t.Key.Wind == _round.Game.GetPlayerCurrentWind(_round.CurrentPlayerIndex))
                    && deadTiles.Count(_ => _ == t.Key) < 2)
                // keeps group of following numbers
                .ThenByDescending(t =>
                {
                    var m2 = concealedTiles.Any(tb => tb.Family == t.Key.Family && tb.Number == t.Key.Number - 2);
                    var m1 = concealedTiles.Any(tb => tb.Family == t.Key.Family && tb.Number == t.Key.Number - 1);
                    var p1 = concealedTiles.Any(tb => tb.Family == t.Key.Family && tb.Number == t.Key.Number + 1);
                    var p2 = concealedTiles.Any(tb => tb.Family == t.Key.Family && tb.Number == t.Key.Number + 2);

                    return ((m1 ? 1 : 0) * 2) + ((p1 ? 1 : 0) * 2) + (p2 ? 1 : 0) + (m2 ? 1 : 0);
                })
                // doras are better than "not dora"
                .ThenByDescending(t => _round.GetDoraCount(t.Key) + (t.Key.IsRedDora ? 1 : 0))
                // keeps pair
                .ThenByDescending(t => t.Count())
                // all things being equal, wind are the best to discard
                .ThenBy(t => t.Key.Family == Families.Wind)
                // all things being equal, the closer to side the better
                .ThenBy(t => t.Key.DistanceToMiddle(false))
                .Reverse();

        return tilesGroup.First(tg => discardableTiles.Contains(tg.Key)).Key;
    }

    /// <inheritdoc />
    public (TilePivot? choice, IReadOnlyList<TilePivot> potentials) RiichiDecision()
    {
        var riichiTiles = _round.CanCallRiichi();

        if (riichiTiles.Count < 2)
            return (riichiTiles.Count > 0 ? riichiTiles[0] : null, riichiTiles);

        var deadTiles = _round.DeadTilesFromIndexPointOfView(_round.CurrentPlayerIndex);

        var (tilesSafety, stopCurrentHand) = ComputeTilesSafety(riichiTiles, deadTiles);

        var tileSelected = GetBestDiscardFromList(riichiTiles, tilesSafety, stopCurrentHand);

        return (tileSelected, riichiTiles);
    }

    /// <inheritdoc />
    public IReadOnlyList<PlayerIndices> RonDecision(bool ronCalled)
    {
        var callers = new List<PlayerIndices>(4);

        foreach (var i in Enum.GetValues<PlayerIndices>())
        {
            if (_round.Game.IsCpu(i) && _round.CanCallRon(i))
            {
                if (ronCalled || callers.Count > 0)
                {
                    callers.Add(i);
                }
                else
                {
                    // Same code as the "if" statement : expected for now.
                    callers.Add(i);
                }
            }
        }

        return callers;
    }

    /// <inheritdoc />
    public bool KanDecisionAdvice(PlayerIndices pIndex, IReadOnlyList<TilePivot> kanPossibilities, bool concealedKan)
        => KanDecisionInternal(pIndex, kanPossibilities, concealedKan) != null;

    /// <inheritdoc />
    public bool PonDecisionAdvice(PlayerIndices pIndex)
        => PonDecisionInternal(pIndex).HasValue;

    /// <inheritdoc />
    public bool ChiiDecisionAdvice(IReadOnlyList<TilePivot> chiiPossibilities)
        => ChiiDecisionInternal(chiiPossibilities) != null;

    /// <inheritdoc />
    public PlayerIndices? PonDecision()
    {
        var opponentPlayerId = _round.OpponentsCanCallPon();
        if (opponentPlayerId.HasValue)
        {
            opponentPlayerId = PonDecisionInternal(opponentPlayerId.Value);
        }

        return opponentPlayerId;
    }

    /// <inheritdoc />
    public bool TsumoDecision(bool isKanCompensation)
    {
        return _round.CanCallTsumo(isKanCompensation);
    }

    /// <inheritdoc />
    public (PlayerIndices pIndex, TilePivot tile)? KanDecision(bool checkConcealedOnly)
    {
        var opponentPlayerIdWithTiles = _round.OpponentsCanCallKan(checkConcealedOnly);
        return opponentPlayerIdWithTiles.HasValue
            ? KanDecisionInternal(opponentPlayerIdWithTiles.Value.Item1, opponentPlayerIdWithTiles.Value.Item2, checkConcealedOnly)
            : null;
    }

    /// <inheritdoc />
    public TilePivot? ChiiDecision()
    {
        var chiiTiles = _round.OpponentsCanCallChii();
        return chiiTiles.Count > 0 ? ChiiDecisionInternal(chiiTiles) : null;
    }

    #region Private methods

    private TilePivot GetBestDiscardFromList(IReadOnlyList<TilePivot> tenpaiPotentialDiscards,
        IReadOnlyList<(TilePivot tile, int unsafePoints)> tilesSafety, bool playsSafe)
    {
        if (tenpaiPotentialDiscards.Count == 1)
            return tenpaiPotentialDiscards[0];

        int safetyFunc(TilePivot t) => tilesSafety.First(_ => _.tile == t).unsafePoints;
        int dorasFunc(TilePivot t) => _round.GetDoraCount(t) + (t.IsRedDora ? 1 : 0);

        var orderedTiles = playsSafe
            ? tenpaiPotentialDiscards
                .OrderBy(safetyFunc)
                .ThenBy(dorasFunc)
            : tenpaiPotentialDiscards
                .OrderBy(dorasFunc)
                .ThenBy(safetyFunc);

        return orderedTiles
            .ThenByDescending(t => t.DistanceToMiddle(true))
            .ThenBy(t => t)
            .First();
    }

    private PlayerIndices? PonDecisionInternal(PlayerIndices playerIndex)
    {
        var tile = _round.GetDiscard(_round.PreviousPlayerIndex)[_round.GetDiscard(_round.PreviousPlayerIndex).Count - 1];

        var hand = _round.GetHand(playerIndex);

        var tenpaiOpponentIndexes = GetTenpaiOpponentIndexes(playerIndex);

        // if the hand is already opened and no opponent tenpai: takes it
        if (!hand.IsConcealed && tenpaiOpponentIndexes.Count == 0 && (!_itsuFamily.HasValue || _itsuFamily == tile.Family))
        {
            return playerIndex;
        }

        var valuableWinds = new[] { _round.Game.GetPlayerCurrentWind(playerIndex), _round.Game.DominantWind };

        var canPonForYakuhai = IsDragonOrValuableWind(tile, valuableWinds);

        // >= 75% of the tile family
        var closeToChinitsu = hand.ConcealedTiles.Count(_ => _.Family == tile.Family) >= 11;

        // how much pair (or better) of valuable honors ?
        var valuableHonorPairs = hand.ConcealedTiles.GroupBy(_ => _)
            .Count(_ => _.Key.IsHonor && _.Count() >= 2 && IsDragonOrValuableWind(_.Key, valuableWinds));

        if (!canPonForYakuhai && valuableHonorPairs < 2 && !closeToChinitsu)
        {
            return null;
        }

        var dorasCount = hand.ConcealedTiles
            .Sum(_round.GetDoraCount);
        var redDorasCount = hand.ConcealedTiles.Count(t => t.IsRedDora);

        var hasValuablePair = hand.ConcealedTiles.GroupBy(_ => _)
            .Any(_ => _.Count() >= 2 && _.Key != tile && IsDragonOrValuableWind(_.Key, valuableWinds));

        // >= 66% of one family or honor
        var closeToHonitsuFamily = new Families?[] { Families.Bamboo, Families.Caracter, Families.Circle }
            .FirstOrDefault(f => hand.ConcealedTiles.Count(t => t.Family == f || t.IsHonor) > 9);

        _itsuFamily ??= closeToHonitsuFamily;

        return hasValuablePair
            || (dorasCount + redDorasCount) > 0
            || closeToHonitsuFamily.HasValue
            || valuableWinds[0] == Winds.East
            ? playerIndex
            : null;
    }

    private (PlayerIndices pIndex, TilePivot tile)? KanDecisionInternal(PlayerIndices playerId, IReadOnlyList<TilePivot> kanPossibilities, bool concealed)
    {
        // Rinshan kaihou possibility: call
        var tileToRemove = _round.GetHand(playerId).IsFullHand
            ? kanPossibilities[0]
            : null;

        var meIsTenpai = _round.IsTenpai(playerId, tileToRemove);
        if (!meIsTenpai && GetTenpaiOpponentIndexes(playerId).Count > 0)
        {
            // riichi or opponents close to win: no call
            return null;
        }

        foreach (var tile in kanPossibilities)
        {
            // Call the kan if :
            // - it's a concealed one
            // - the hand is already open
            if (concealed
                || (!_round.GetHand(playerId).IsConcealed && (!_itsuFamily.HasValue || _itsuFamily == tile.Family)))
            {
                return (playerId, tile);
            }
        }

        return null;
    }

    private TilePivot? ChiiDecisionInternal(IReadOnlyList<TilePivot> chiiTiles)
    {
        // Proceeds to chii if :
        // - The hand is already open (we assume it's open for a good reason)
        // - The sequence does not already exist in the end
        // - Nobody is tenpai
        // - if a honiisou or better is in progress, tile to chii should be of this family
        var tenpaiOppenentIndexes = GetTenpaiOpponentIndexes(_round.CurrentPlayerIndex);

        if (_round.GetHand(_round.CurrentPlayerIndex).IsConcealed
            || tenpaiOppenentIndexes.Count > 0
            || (_itsuFamily.HasValue && _itsuFamily != chiiTiles[0].Family))
        {
            return null;
        }

        TilePivot? tileChoice = null;
        foreach (var tileKey in chiiTiles)
        {
            var m2 = _round.GetHand(_round.CurrentPlayerIndex).ConcealedTiles.Any(t => t.Family == tileKey.Family && t.Number == tileKey.Number - 2);
            var m1 = _round.GetHand(_round.CurrentPlayerIndex).ConcealedTiles.Any(t => t.Family == tileKey.Family && t.Number == tileKey.Number - 1);
            var m0 = _round.GetHand(_round.CurrentPlayerIndex).ConcealedTiles.Any(t => t == tileKey);
            var p1 = _round.GetHand(_round.CurrentPlayerIndex).ConcealedTiles.Any(t => t.Family == tileKey.Family && t.Number == tileKey.Number + 1);
            var p2 = _round.GetHand(_round.CurrentPlayerIndex).ConcealedTiles.Any(t => t.Family == tileKey.Family && t.Number == tileKey.Number + 2);

            if (!((m2 && m1 && m0) || (m1 && m0 && p1) || (m0 && p1 && p2)))
            {
                tileChoice = tileKey;
            }
        }

        return tileChoice;
    }

    private bool IsDiscardedOrUnusable(TilePivot tile, PlayerIndices opponentPlayerIndex, IReadOnlyList<TilePivot> deadtiles)
    {
        return _round.GetDiscard(opponentPlayerIndex).Contains(tile) || (
            tile.IsHonor
            && deadtiles.Count(t => t == tile) == 3
            // this last line is to avoid giving Kokushi musou tile
            && deadtiles.GroupBy(t => t).Any(t => t.Key != tile && t.Key.IsHonor && t.Count() > 2)
        );
    }

    // suji on the middle (least safe)
    private bool IsInsiderSuji(TilePivot tile, PlayerIndices opponentPlayerIndex)
    {
        return GetSujisFromDiscard(tile, opponentPlayerIndex)
            .Any(_ => !MiddleNumbers.Contains(_.Number));
    }

    // suji on the edge (safer)
    private bool IsOutsiderSuji(TilePivot tile, PlayerIndices opponentPlayerIndex)
    {
        return GetSujisFromDiscard(tile, opponentPlayerIndex)
            .Any(_ => MiddleNumbers.Contains(_.Number));
    }

    // suji on both side
    private bool IsDoubleInsiderSuji(TilePivot tile, PlayerIndices opponentPlayerIndex)
    {
        return GetSujisFromDiscard(tile, opponentPlayerIndex).Distinct().Count() > 1;
    }

    private IEnumerable<TilePivot> GetSujisFromDiscard(TilePivot tile, PlayerIndices opponentPlayerIndex)
    {
        return tile.IsHonor
            ? Enumerable.Empty<TilePivot>()
            : _round
            .GetDiscard(opponentPlayerIndex)
            .Where(_ => _.Family == tile.Family && (_.Number == tile.Number + 3 || _.Number == tile.Number - 3));
    }

    // a family is over-represented in the opponent discard (at least 9 overal, and at least 3 distinct values)
    private bool IsMaxedFamilyInDiscard(TilePivot tile, PlayerIndices opponentPlayerIndex)
    {
        return !tile.IsHonor && _round.GetDiscard(opponentPlayerIndex).Count(_ => _.Family == tile.Family) >= 9
            && _round.GetDiscard(opponentPlayerIndex).Where(_ => _.Family == tile.Family).Distinct().Count() >= 3;
    }

    private static bool IsDragonOrValuableWind(TilePivot tile, Winds[] winds)
    {
        return tile.Family == Families.Dragon
            || (tile.Family == Families.Wind && winds.Contains(tile.Wind!.Value));
    }

    private (IReadOnlyList<(TilePivot tile, int unsafePoints)> bestToWorstChoices, bool shouldGiveUp) ComputeTilesSafety(IReadOnlyList<TilePivot> discardableTiles, IReadOnlyList<TilePivot> deadTiles)
    {
        var tilesSafety = new Dictionary<TilePivot, List<TileSafety>>();

        // computes once
        var tenpaiOpponentIndexes = GetTenpaiOpponentIndexes(_round.CurrentPlayerIndex);

        var stopCurrentHand = false;
        foreach (var tile in discardableTiles)
        {
            tilesSafety.Add(tile, new List<TileSafety>(3));
            foreach (var i in Enum.GetValues<PlayerIndices>().Where(i => i != _round.CurrentPlayerIndex))
            {
                // stop the building of the hand is opponent is riichi or has 3 or more combinations visible
                if (tenpaiOpponentIndexes.Contains(i))
                {
                    stopCurrentHand = true;
                    if (IsDiscardedOrUnusable(tile, i, deadTiles))
                    {
                        tilesSafety[tile].Add(TileSafety.Safe);
                    }
                    else if (IsOutsiderSuji(tile, i) || IsDoubleInsiderSuji(tile, i) || IsMaxedFamilyInDiscard(tile, i))
                    {
                        tilesSafety[tile].Add(TileSafety.QuiteSafe);
                    }
                    else if (IsInsiderSuji(tile, i) || tile.IsHonorOrTerminal)
                    {
                        tilesSafety[tile].Add(TileSafety.QuiteUnsafe);
                    }
                    else
                    {
                        tilesSafety[tile].Add(TileSafety.Unsafe);
                    }
                }
                else if (_round.GetDoraCount(tile) > 0)
                {
                    // dora are bit unsafe at first, then unsafe at middle game
                    tilesSafety[tile].Add(_round.WallTiles.Count > 42 ? TileSafety.QuiteUnsafe : TileSafety.Unsafe);
                }
                else
                {
                    tilesSafety[tile].Add(TileSafety.AverageOrUnknown);
                }
            }
        }

        return (tilesSafety.OrderBy(t => t.Value.Sum(s => (int)s)).Select(t => (t.Key, t.Value.Sum(s => (int)s))).ToList(), stopCurrentHand);
    }

    private List<PlayerIndices> GetTenpaiOpponentIndexes(PlayerIndices playerIndex)
        => Enum.GetValues<PlayerIndices>().Where(i => i != playerIndex && PlayerIsCloseToWin(i)).ToList();

    private bool PlayerIsCloseToWin(PlayerIndices i) => _round.IsRiichi(i) || _round.GetHand(i).DeclaredCombinations.Count > 2;

    #endregion Private methods

    /// <summary>
    /// Enumeration of level of safety for tile discard.
    /// </summary>
    internal enum TileSafety
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
