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
        List<TilePivot> discardableTiles,
        IReadOnlyList<TilePivot>? knownTenpaiDiscardChoices)
    {
        var deadTiles = Round.DeadTilesFromIndexPointOfView(Round.CurrentPlayerIndex);

        var (tilesSafety, stopCurrentHand) = ComputeTilesSafety(discardableTiles, deadTiles);

        // tenpai: let's go anyway...
        var tenpaiPotentialDiscards = knownTenpaiDiscardChoices ?? Round.ExtractDiscardChoicesFromTenpai(Round.CurrentPlayerIndex);
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
                // once committed to honitsu/chinitsu (see _itsuFamily), tiles from any other family
                // are the very first to go: they can no longer be turned into a call (Pon/Kan/Chii
                // are all gated on this same family), so they're pure dead weight from here on.
                .OrderByDescending(t => !_itsuFamily.HasValue || t.Key.Family == _itsuFamily)
                // keeps brelan/square
                .ThenByDescending(t =>
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
                // keeps the best shape this tile takes part in: an open-ended ryanmen (e.g. 2-3, waits
                // on two tile types) beats an ordinary pair (backup pair / shanpon / triplet potential),
                // which itself beats a one-sided kanchan/penchan (e.g. 1-3 or 1-2, waits on a single
                // tile type) or an isolated tile with no shape at all.
                .ThenByDescending(t => TaatsuQuality(t.Key, concealedTiles))
                // doras are better than "not dora"
                .ThenByDescending(t => Round.GetDoraCount(t.Key) + (t.Key.IsRedDora ? 1 : 0))
                // all things being equal, wind are the best to discard
                .ThenBy(t => t.Key.Family == Families.Wind)
                // all things being equal, the closer to side the better (an isolated honor counts as
                // further from the middle than a terminal, matching GetBestDiscardFromList below)
                .ThenBy(t => t.Key.DistanceToMiddle(true))
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

    protected override bool PonDecisionInternal(
        PlayerIndices playerIndex)
    {
        var tile = Round.GetDiscard(Round.PreviousPlayerIndex)[Round.GetDiscard(Round.PreviousPlayerIndex).Count - 1];

        var hand = Round.GetHand(playerIndex);

        var tenpaiOpponentIndexes = GetTenpaiOpponentIndexes(playerIndex);

        // if the hand is already opened and no opponent tenpai: takes it
        if (!hand.IsConcealed && tenpaiOpponentIndexes.Count == 0 && (!_itsuFamily.HasValue || _itsuFamily == tile.Family))
        {
            return true;
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
            return false;
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
            || valuableWinds[0] == Winds.East;
    }

    protected override TilePivot? KanDecisionInternal(
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
                return tile;
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

        var concealedTiles = Round.GetHand(Round.CurrentPlayerIndex).ConcealedTiles;
        var discardedTile = Round.GetDiscard(Round.PreviousPlayerIndex)[^1];

        TilePivot? tileChoice = null;
        var bestCost = int.MaxValue;
        foreach (var tileKey in chiiTiles)
        {
            var m2 = concealedTiles.Any(t => t.Family == tileKey.Family && t.Number == tileKey.Number - 2);
            var m1 = concealedTiles.Any(t => t.Family == tileKey.Family && t.Number == tileKey.Number - 1);
            var m0 = concealedTiles.Any(t => t == tileKey);
            var p1 = concealedTiles.Any(t => t.Family == tileKey.Family && t.Number == tileKey.Number + 1);
            var p2 = concealedTiles.Any(t => t.Family == tileKey.Family && t.Number == tileKey.Number + 2);

            if ((m2 && m1 && m0) || (m1 && m0 && p1) || (m0 && p1 && p2))
            {
                // would break a run already complete in hand: never worth it
                continue;
            }

            // among several valid sequences, prefer the one that sacrifices the least valuable pair
            // of tiles from the concealed hand (the discarded tile itself costs nothing, it's free).
            // "tileKey" is only ever discard-2, discard-1 or discard+1 (see CanCallChii): the other
            // hand tile of the sequence is deduced from that same offset.
            var companionNumber = tileKey.Number - discardedTile.Number == -1
                ? tileKey.Number + 2
                : tileKey.Number + 1;
            var companion = concealedTiles.First(t => t.Family == tileKey.Family && t.Number == companionNumber);
            var cost = TileKeepValue(tileKey) + TileKeepValue(companion);

            if (cost < bestCost)
            {
                bestCost = cost;
                tileChoice = tileKey;
            }
        }

        return tileChoice;
    }

    // Tiered "shape quality" for a tile value: an open-ended ryanmen (e.g. 2-3, waits on two tile
    // types) beats an ordinary pair (backup pair / shanpon / triplet potential), which itself beats
    // a one-sided kanchan/penchan (e.g. 1-3 or 1-2, waits on a single tile type), which beats an
    // isolated tile with no shape at all.
    private int TaatsuQuality(TilePivot key, IReadOnlyList<TilePivot> concealedTiles)
    {
        var m2 = concealedTiles.Any(t => t.Family == key.Family && t.Number == key.Number - 2);
        var m1 = concealedTiles.Any(t => t.Family == key.Family && t.Number == key.Number - 1);
        var p1 = concealedTiles.Any(t => t.Family == key.Family && t.Number == key.Number + 1);
        var p2 = concealedTiles.Any(t => t.Family == key.Family && t.Number == key.Number + 2);

        // a consecutive pair (n, n+1) is a ryanmen unless it sits on the 1-2 or 8-9 edge (penchan)
        var ryanmenAsUpperTile = m1 && key.Number is >= 3 and <= 8;
        var ryanmenAsLowerTile = p1 && key.Number is >= 2 and <= 7;

        if (ryanmenAsUpperTile || ryanmenAsLowerTile)
        {
            return 3;
        }

        if (concealedTiles.Count(t => t == key) > 1)
        {
            return 2;
        }

        if (m1 || p1 || m2 || p2)
        {
            return 1;
        }

        return 0;
    }

    // Rough "worth keeping" score for a tile about to be spent on a chii call: doras are the obvious
    // loss, and a tile already paired in hand has follow-up potential (yakuhai, toitoi, extra brelan)
    // that a lone sequence tile doesn't.
    private int TileKeepValue(TilePivot tile)
    {
        var dorasValue = Round.GetDoraCount(tile) + (tile.IsRedDora ? 1 : 0);
        var pairValue = Round.GetHand(Round.CurrentPlayerIndex).ConcealedTiles.Count(t => t == tile) > 1 ? 1 : 0;
        return dorasValue + pairValue;
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

        // Precomputed once per opponent (not once per candidate tile): every safety check below only
        // ever reads the opponent's discard pile, which doesn't change while we're evaluating it here.
        var opponentSummaries = tenpaiOpponentIndexes.ToDictionary(i => i, i => new OpponentDiscardSummary(Round.GetDiscard(i)));

        var stopCurrentHand = false;
        foreach (var tile in discardableTiles)
        {
            tilesSafety.Add(tile, new List<TileSafety>(3));

            // Also opponent-independent, and also constant across the whole tile loop, but cheap
            // enough that a per-tile computation (rather than per-tile-per-opponent) is good enough.
            var honorIsolatedAndVisible = tile.IsHonor
                && deadTiles.Count(t => t == tile) == 4
                && deadTiles.GroupBy(t => t).Any(g => g.Key != tile && g.Key.IsHonorOrTerminal && g.Count() > 3);

            foreach (var i in Enum.GetValues<PlayerIndices>().Where(i => i != Round.CurrentPlayerIndex))
            {
                // stop the building of the hand is opponent is riichi or has 3 or more combinations visible
                if (tenpaiOpponentIndexes.Contains(i))
                {
                    stopCurrentHand = true;
                    var summary = opponentSummaries[i];
                    if (summary.Tiles.Contains(tile) || honorIsolatedAndVisible)
                    {
                        tilesSafety[tile].Add(TileSafety.Safe);
                    }
                    else if (summary.IsOutsiderSuji(tile, MiddleNumbers) || summary.IsDoubleInsiderSuji(tile) || summary.IsMaxedFamily(tile))
                    {
                        tilesSafety[tile].Add(TileSafety.QuiteSafe);
                    }
                    else if (summary.IsInsiderSuji(tile, MiddleNumbers) || tile.IsHonorOrTerminal)
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

    // Precomputed summary of one opponent's discard pile, so every per-candidate-tile safety check
    // below is an O(1) lookup instead of a fresh LINQ scan of the discard list every time.
    // Reproduces exactly CpuManagerBasePivot.IsGuaranteedSafe/IsInsiderSuji/IsOutsiderSuji/
    // IsDoubleInsiderSuji/GetSujisFromDiscard and this class's own IsMaxedFamilyInDiscard, which are
    // left untouched (still used verbatim by anything outside this one hot path).
    private sealed class OpponentDiscardSummary
    {
        internal HashSet<TilePivot> Tiles { get; } = new();
        private readonly Dictionary<Families, HashSet<byte>> _numbersByFamily = new();
        private readonly Dictionary<Families, int> _countByFamily = new();

        internal OpponentDiscardSummary(IReadOnlyList<TilePivot> discard)
        {
            foreach (var tile in discard)
            {
                Tiles.Add(tile);

                if (!_numbersByFamily.TryGetValue(tile.Family, out var numbers))
                {
                    numbers = new HashSet<byte>();
                    _numbersByFamily[tile.Family] = numbers;
                }
                numbers.Add(tile.Number);

                _countByFamily[tile.Family] = _countByFamily.GetValueOrDefault(tile.Family) + 1;
            }
        }

        private bool HasNumber(Families family, int number)
            => number is >= 1 and <= 9 && _numbersByFamily.TryGetValue(family, out var numbers) && numbers.Contains((byte)number);

        internal bool IsInsiderSuji(TilePivot tile, int[] middleNumbers)
            => !tile.IsHonor && (
                (HasNumber(tile.Family, tile.Number + 3) && !middleNumbers.Contains(tile.Number + 3))
                || (HasNumber(tile.Family, tile.Number - 3) && !middleNumbers.Contains(tile.Number - 3)));

        internal bool IsOutsiderSuji(TilePivot tile, int[] middleNumbers)
            => !tile.IsHonor && (
                (HasNumber(tile.Family, tile.Number + 3) && middleNumbers.Contains(tile.Number + 3))
                || (HasNumber(tile.Family, tile.Number - 3) && middleNumbers.Contains(tile.Number - 3)));

        internal bool IsDoubleInsiderSuji(TilePivot tile)
            => !tile.IsHonor && HasNumber(tile.Family, tile.Number + 3) && HasNumber(tile.Family, tile.Number - 3);

        internal bool IsMaxedFamily(TilePivot tile)
            => !tile.IsHonor
                && _countByFamily.GetValueOrDefault(tile.Family) >= 9
                && _numbersByFamily.TryGetValue(tile.Family, out var numbers) && numbers.Count >= 3;
    }

    private List<PlayerIndices> GetTenpaiOpponentIndexes(PlayerIndices playerIndex)
        => Enum.GetValues<PlayerIndices>().Where(i => i != playerIndex && PlayerIsCloseToWin(i)).ToList();

    private bool PlayerIsCloseToWin(PlayerIndices i)
        => Round.IsRiichi(i) || Round.GetHand(i).DeclaredCombinations.Count > 2;

    #endregion Private methods
}
