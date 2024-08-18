using Gnoj_Ham_Library.Enums;

namespace Gnoj_Ham_Library;

/// <summary>
/// Basic implementation of <see cref="ICpuDecisionsManagerPivot"/>.
/// </summary>
public class BasicCpuManagerPivot : CpuManagerBasePivot
{
    // indicates an honiisou (or chiniisou) in progress
    private Families? _itsuFamily;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="round">The <see cref="_round"/> value.</param>
    internal BasicCpuManagerPivot(RoundPivot round)
        : base(round)
    { }

    protected override TilePivot DiscardDecisionInternal(
        IReadOnlyList<TilePivot> concealedTiles,
        List<TilePivot> discardableTiles)
    {
        var deadTiles = Round.DeadTilesFromIndexPointOfView(Round.CurrentPlayerIndex);

        var (tilesSafety, stopCurrentHand) = ComputeTilesSafety(discardableTiles, deadTiles);

        // tenpai: let's go anyway...
        var tenpaiPotentialDiscards = Round.ExtractDiscardChoicesFromTenpai(Round.CurrentPlayerIndex);
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
                        || t.Key.Wind == Round.Game.DominantWind
                        || t.Key.Wind == Round.Game.GetPlayerCurrentWind(Round.CurrentPlayerIndex))
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
                .ThenByDescending(t => Round.GetDoraCount(t.Key) + (t.Key.IsRedDora ? 1 : 0))
                // keeps pair
                .ThenByDescending(t => t.Count())
                // all things being equal, wind are the best to discard
                .ThenBy(t => t.Key.Family == Families.Wind)
                // all things being equal, the closer to side the better
                .ThenBy(t => t.Key.DistanceToMiddle(false))
                .Reverse();

        return tilesGroup.First(tg => discardableTiles.Contains(tg.Key)).Key;
    }

    protected override TilePivot RiichiDecisionInternal(
        IReadOnlyList<TilePivot> riichiTiles)
    {
        if (riichiTiles.Count == 1)
            return riichiTiles[0];

        var deadTiles = Round.DeadTilesFromIndexPointOfView(Round.CurrentPlayerIndex);

        var (tilesSafety, stopCurrentHand) = ComputeTilesSafety(riichiTiles, deadTiles);

        var tileSelected = GetBestDiscardFromList(riichiTiles, tilesSafety, stopCurrentHand);

        return tileSelected;
    }

    protected override PlayerIndices? PonDecisionInternal(
        PlayerIndices playerIndex)
    {
        var tile = Round.GetDiscard(Round.PreviousPlayerIndex)[Round.GetDiscard(Round.PreviousPlayerIndex).Count - 1];

        var hand = Round.GetHand(playerIndex);

        var tenpaiOpponentIndexes = GetTenpaiOpponentIndexes(playerIndex);

        // if the hand is already opened and no opponent tenpai: takes it
        if (!hand.IsConcealed && tenpaiOpponentIndexes.Count == 0 && (!_itsuFamily.HasValue || _itsuFamily == tile.Family))
        {
            return playerIndex;
        }

        var valuableWinds = new[] { Round.Game.GetPlayerCurrentWind(playerIndex), Round.Game.DominantWind };

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
            .Sum(Round.GetDoraCount);
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

    protected override (PlayerIndices playerIndex, TilePivot tile)? KanDecisionInternal(
        PlayerIndices playerIndex,
        IReadOnlyList<TilePivot> kanPossibilities,
        bool concealed)
    {
        // Rinshan kaihou possibility: call
        var tileToRemove = Round.GetHand(playerIndex).IsFullHand
            ? kanPossibilities[0]
            : null;

        var meIsTenpai = Round.IsTenpai(playerIndex, tileToRemove);
        if (!meIsTenpai && GetTenpaiOpponentIndexes(playerIndex).Count > 0)
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
                || (!Round.GetHand(playerIndex).IsConcealed && (!_itsuFamily.HasValue || _itsuFamily == tile.Family)))
            {
                return (playerIndex, tile);
            }
        }

        return null;
    }

    protected override TilePivot? ChiiDecisionInternal(
        IReadOnlyList<TilePivot> chiiTiles)
    {
        // Proceeds to chii if :
        // - The hand is already open (we assume it's open for a good reason)
        // - The sequence does not already exist in the end
        // - Nobody is tenpai
        // - if a honiisou or better is in progress, tile to chii should be of this family
        var tenpaiOppenentIndexes = GetTenpaiOpponentIndexes(Round.CurrentPlayerIndex);

        if (Round.GetHand(Round.CurrentPlayerIndex).IsConcealed
            || tenpaiOppenentIndexes.Count > 0
            || (_itsuFamily.HasValue && _itsuFamily != chiiTiles[0].Family))
        {
            return null;
        }

        TilePivot? tileChoice = null;
        foreach (var tileKey in chiiTiles)
        {
            var m2 = Round.GetHand(Round.CurrentPlayerIndex).ConcealedTiles.Any(t => t.Family == tileKey.Family && t.Number == tileKey.Number - 2);
            var m1 = Round.GetHand(Round.CurrentPlayerIndex).ConcealedTiles.Any(t => t.Family == tileKey.Family && t.Number == tileKey.Number - 1);
            var m0 = Round.GetHand(Round.CurrentPlayerIndex).ConcealedTiles.Any(t => t == tileKey);
            var p1 = Round.GetHand(Round.CurrentPlayerIndex).ConcealedTiles.Any(t => t.Family == tileKey.Family && t.Number == tileKey.Number + 1);
            var p2 = Round.GetHand(Round.CurrentPlayerIndex).ConcealedTiles.Any(t => t.Family == tileKey.Family && t.Number == tileKey.Number + 2);

            if (!((m2 && m1 && m0) || (m1 && m0 && p1) || (m0 && p1 && p2)))
            {
                tileChoice = tileKey;
            }
        }

        return tileChoice;
    }

    #region Private methods

    private TilePivot GetBestDiscardFromList(IReadOnlyList<TilePivot> tenpaiPotentialDiscards,
        IReadOnlyList<(TilePivot tile, int unsafePoints)> tilesSafety, bool playsSafe)
    {
        if (tenpaiPotentialDiscards.Count == 1)
            return tenpaiPotentialDiscards[0];

        int safetyFunc(TilePivot t) => tilesSafety.First(_ => _.tile == t).unsafePoints;
        int dorasFunc(TilePivot t) => Round.GetDoraCount(t) + (t.IsRedDora ? 1 : 0);

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

    // a family is over-represented in the opponent discard (at least 9 overal, and at least 3 distinct values)
    private bool IsMaxedFamilyInDiscard(TilePivot tile, PlayerIndices opponentPlayerIndex)
    {
        return !tile.IsHonor && Round.GetDiscard(opponentPlayerIndex).Count(_ => _.Family == tile.Family) >= 9
            && Round.GetDiscard(opponentPlayerIndex).Where(_ => _.Family == tile.Family).Distinct().Count() >= 3;
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
        var tenpaiOpponentIndexes = GetTenpaiOpponentIndexes(Round.CurrentPlayerIndex);

        var stopCurrentHand = false;
        foreach (var tile in discardableTiles)
        {
            tilesSafety.Add(tile, new List<TileSafety>(3));
            foreach (var i in Enum.GetValues<PlayerIndices>().Where(i => i != Round.CurrentPlayerIndex))
            {
                // stop the building of the hand is opponent is riichi or has 3 or more combinations visible
                if (tenpaiOpponentIndexes.Contains(i))
                {
                    stopCurrentHand = true;
                    if (IsGuaranteedSafe(tile, i, deadTiles))
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
                else if (Round.GetDoraCount(tile) > 0)
                {
                    // dora are bit unsafe at first, then unsafe at middle game
                    tilesSafety[tile].Add(Round.WallTiles.Count > 42 ? TileSafety.QuiteUnsafe : TileSafety.Unsafe);
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

    private bool PlayerIsCloseToWin(PlayerIndices i)
        => Round.IsRiichi(i) || Round.GetHand(i).DeclaredCombinations.Count > 2;

    #endregion Private methods
}
