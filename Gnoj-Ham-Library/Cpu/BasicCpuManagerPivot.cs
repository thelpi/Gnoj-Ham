using Gnoj_Ham_Library.Enums;

namespace Gnoj_Ham_Library;

/// <summary>
/// Basic implementation of <see cref="ICpuDecisionsManagerPivot"/>.
/// </summary>
public class BasicCpuManagerPivot : CpuManagerBasePivot
{
    // the 13 distinct tile kinds a kokushi musou needs: 1 and 9 of each suit, plus every honor.
    private static readonly IReadOnlyList<TilePivot> KokushiKinds =
        TilePivot.GetCompleteSet(false).Where(t => t.IsHonorOrTerminal).Distinct().ToList();

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="round">The <see cref="_round"/> value.</param>
    public BasicCpuManagerPivot(RoundPivot round)
        : base(round)
    { }

    // A bare-minimum 9-or-10-kinds start is too speculative to chase over just restarting the deal.
    // From 11 kinds on (kokushi shanten 1 or better, counting a possible pair among them), the hand
    // is worth pursuing instead - see CloseToKokushi, which then takes over the discard priority.
    protected override bool KyuushuKyuuhaiDecisionInternal()
    {
        var kindsCount = Round.GetHand(Round.CurrentPlayerIndex).ConcealedTiles
            .Where(t => t.IsHonorOrTerminal).Distinct().Count();
        return kindsCount <= 10;
    }

    // Whether the hand is worth prioritizing for kokushi musou: enough distinct terminal/honor kinds
    // held (same bar as declining Kyuushu Kyuuhai - see KyuushuKyuuhaiDecisionInternal), AND every
    // one of the 13 needed kinds still theoretically reachable (not entirely exhausted among tiles
    // visible to us: discards, melds, dora indicators). Recomputed fresh from the current hand and
    // dead tiles every time it's needed, so the pursuit drops on its own once it stops making sense -
    // no persisted "commitment" flag to go stale (same reasoning as CloseToHonitsuFamily).
    private static bool CloseToKokushi(IReadOnlyList<TilePivot> concealedTiles, IReadOnlyList<TilePivot> deadTiles)
    {
        var heldKinds = concealedTiles.Where(t => t.IsHonorOrTerminal).Distinct().ToList();
        if (heldKinds.Count < 11)
        {
            return false;
        }

        return KokushiKinds.All(k => heldKinds.Contains(k) || deadTiles.Count(t => t == k) < TilePivot.CopiesCount);
    }

    // Dedicated discard logic for an active kokushi musou pursuit (see CloseToKokushi): a special
    // hand shape where none of the normal shape-based criteria (taatsu, triplets, honitsu family...)
    // make sense - they're built around forming sequences/triplets/one pair per group, whereas kokushi
    // only ever wants exactly one copy of each of the 13 kinds plus a single pair. Folding it into
    // that same generic scoring chain would let it misfire (e.g. the "keeps brelan" criterion would
    // protect a 3rd copy of a wind over a still-needed lone kind), so it's kept fully independent.
    private static TilePivot KokushiDiscardDecision(IReadOnlyList<TilePivot> concealedTiles, List<TilePivot> discardableTiles)
    {
        // simple (non-honor/terminal) tiles are pure dead weight for kokushi: always discard first.
        var simple = discardableTiles.FirstOrDefault(t => !t.IsHonorOrTerminal);
        if (simple != null)
        {
            return simple;
        }

        // every held tile is honor/terminal: trim the most redundant kind first (a 3rd copy of a
        // wind, or - failing that - one of several pairs, since only one pair is ever useful), never
        // a still-lone kind, which is exactly the coverage kokushi is trying to build.
        var mostRedundant = concealedTiles
            .GroupBy(t => t)
            .Where(g => g.Count() >= 2)
            .OrderByDescending(g => g.Count())
            .First();

        return discardableTiles.First(t => t == mostRedundant.Key);
    }

    // Worth prioritizing chiitoitsu (seven pairs): at least 5 distinct pairs already, not building
    // toward toitoi/sanankou instead (2+ triplets - a triplet is dead weight here, a third copy can
    // never become a second distinct pair), fewer than 3 complete sequences found in an alternate,
    // non-pair reading of the same tiles (a hand that reads just as well as a near-complete normal
    // hand shouldn't be pulled toward chiitoitsu instead), and not already shaped for iipeikou (two
    // identical sequences), which chiitoitsu can't coexist with either. A hand exactly at 6 pairs is
    // tenpai already (handled upstream by the normal tenpai branch) - this only ever matters for the
    // non-tenpai case, so 5 is the meaningful bar here. Recomputed fresh from the current hand every
    // time it's needed, same reasoning as CloseToHonitsuFamily and CloseToKokushi.
    private static bool CloseToChiitoitsu(IReadOnlyList<TilePivot> concealedTiles)
    {
        var pairsCount = concealedTiles.GroupBy(t => t).Count(g => g.Count() >= 2);
        if (pairsCount < 5)
        {
            return false;
        }

        var triplesCount = concealedTiles.GroupBy(t => t).Count(g => g.Count() >= 3);
        if (triplesCount >= 2)
        {
            return false;
        }

        var (completeSuitesCount, hasIipeikou) = CountCompleteSuites(concealedTiles);
        return completeSuitesCount < 3 && !hasIipeikou;
    }

    // Greedy, non-overlapping count of complete sequences per suit family, reading the hand's tiles
    // as runs rather than pairs - plus whether two of those sequences turn out identical (the shape
    // iipeikou needs, which chiitoitsu can't share tiles with either).
    private static (int count, bool hasDuplicate) CountCompleteSuites(IReadOnlyList<TilePivot> concealedTiles)
    {
        var foundSuites = new List<(Families family, byte start)>();

        foreach (var family in new[] { Families.Caracter, Families.Circle, Families.Bamboo })
        {
            var remaining = concealedTiles.Where(t => t.Family == family).Select(t => t.Number).OrderBy(n => n).ToList();
            for (byte n = 1; n <= 7; n++)
            {
                while (remaining.Contains(n) && remaining.Contains((byte)(n + 1)) && remaining.Contains((byte)(n + 2)))
                {
                    remaining.Remove(n);
                    remaining.Remove((byte)(n + 1));
                    remaining.Remove((byte)(n + 2));
                    foundSuites.Add((family, n));
                }
            }
        }

        return (foundSuites.Count, foundSuites.GroupBy(f => f).Any(g => g.Count() >= 2));
    }

    // Dedicated discard logic for an active chiitoitsu pursuit (see CloseToChiitoitsu): like kokushi,
    // a special shape the generic criteria don't fit - a triplet is actively harmful here (a 3rd
    // copy can never count as a second pair), and taatsu/ryanmen shape is irrelevant.
    private static TilePivot ChiitoitsuDiscardDecision(IReadOnlyList<TilePivot> concealedTiles, List<TilePivot> discardableTiles, IReadOnlyList<TilePivot> deadTiles)
    {
        // a 3rd (or 4th) copy of an already-paired kind is dead weight: it can never become a second
        // distinct pair, so it's always the first to go.
        var overPaired = concealedTiles.GroupBy(t => t).Where(g => g.Count() >= 3).OrderByDescending(g => g.Count()).FirstOrDefault();
        if (overPaired != null)
        {
            return discardableTiles.First(t => t == overPaired.Key);
        }

        // otherwise, every held tile is either a genuine pair (keep) or a still-lone single: discard
        // the single with the fewest remaining live copies elsewhere - it's the least likely to ever
        // actually pair up.
        var singles = concealedTiles.GroupBy(t => t).Where(g => g.Count() == 1).Select(g => g.Key).ToList();
        if (singles.Count > 0)
        {
            var worst = singles.OrderBy(k => TilePivot.CopiesCount - 1 - deadTiles.Count(t => t == k)).First();
            return discardableTiles.First(t => t == worst);
        }

        // every kind already paired (would mean a complete/won hand): shouldn't happen here, but
        // stay safe rather than throw.
        return discardableTiles[0];
    }

    // True while either kokushi musou or chiitoitsu is being pursued (see CloseToKokushi and
    // CloseToChiitoitsu): both require a fully concealed hand, so no Pon/Chii/Kan call should ever
    // be accepted while either is in play - even a concealed kan, which would still burn two tiles
    // that could have gone toward two other needed kinds (kokushi), or is outright illegal for
    // chiitoitsu (which forbids any group of four identical tiles).
    private static bool PursuingClosedHandOnly(IReadOnlyList<TilePivot> concealedTiles, IReadOnlyList<TilePivot> deadTiles)
        => CloseToKokushi(concealedTiles, deadTiles) || CloseToChiitoitsu(concealedTiles);

    protected override TilePivot DiscardDecisionInternal(
        IReadOnlyList<TilePivot> concealedTiles,
        List<TilePivot> discardableTiles,
        IReadOnlyList<TilePivot>? knownTenpaiDiscardChoices)
    {
        var deadTiles = Round.DeadTilesFromIndexPointOfView(Round.CurrentPlayerIndex);

        var (tilesSafety, stopCurrentHand) = ComputeTilesSafety(discardableTiles, deadTiles);

        // Overridden by a stricter variant (see FullDefenseCpuManagerPivot) that folds outright the
        // moment any opponent looks dangerous, even breaking an already-reached tenpai to do it -
        // unlike the tenpai branch right below, which only ever trims its choice toward the safer of
        // the tenpai-preserving discards, never abandons tenpai itself.
        if (stopCurrentHand && AbandonsHandEvenIfTenpai)
        {
            return tilesSafety[0].tile;
        }

        // tenpai: let's go anyway...
        var tenpaiPotentialDiscards = knownTenpaiDiscardChoices ?? Round.ExtractDiscardChoicesFromTenpai(Round.CurrentPlayerIndex);
        if (tenpaiPotentialDiscards.Count > 0)
        {
            return GetBestDiscardFromList(PreferNonFuriten(tenpaiPotentialDiscards), tilesSafety, stopCurrentHand, deadTiles);
        }

        if (stopCurrentHand)
        {
            return tilesSafety[0].tile;
        }

        // kokushi musou and chiitoitsu pursuits are special hand shapes that play by entirely
        // different rules from here on - see KokushiDiscardDecision/ChiitoitsuDiscardDecision for why
        // they're kept independent of the chain below.
        if (CloseToKokushi(concealedTiles, deadTiles))
        {
            return KokushiDiscardDecision(concealedTiles, discardableTiles);
        }

        if (CloseToChiitoitsu(concealedTiles))
        {
            return ChiitoitsuDiscardDecision(concealedTiles, discardableTiles, deadTiles);
        }

        return DevelopmentDiscardDecision(concealedTiles, discardableTiles, deadTiles);
    }

    // What to discard to develop a hand that's neither tenpai, nor giving up, nor a special shape: the
    // general ranking of the tiles by how much they're worth keeping. Virtual so a variant (see
    // EfficiencyCpuManagerPivot) can replace this one step, and keep everything else as it is.
    protected virtual TilePivot DevelopmentDiscardDecision(
        IReadOnlyList<TilePivot> concealedTiles,
        List<TilePivot> discardableTiles,
        IReadOnlyList<TilePivot> deadTiles)
    {
        var itsuFamily = CloseToHonitsuFamily(concealedTiles);

        var tilesGroup =
            concealedTiles
                .GroupBy(t => t)
                // once close to honitsu/chinitsu (see CloseToHonitsuFamily), tiles from any other
                // family are the very first to go: they can no longer be turned into a call (Pon/Kan/
                // Chii are all gated on this same family), so they're pure dead weight from here on.
                // Honors are exempt: honitsu allows the chosen suit plus any honor. Recomputed fresh
                // from the current hand every time (see CloseToHonitsuFamily), so a hand that drifts
                // away from honitsu stops being treated as one, instead of staying locked in forever.
                .OrderByDescending(t => !itsuFamily.HasValue || t.Key.Family == itsuFamily || t.Key.IsHonor)
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

        var tileSelected = GetBestDiscardFromList(PreferNonFuriten(riichiTiles), tilesSafety, stopCurrentHand, deadTiles);

        return tileSelected;
    }

    protected override bool PonDecisionInternal(
        PlayerIndices playerIndex)
    {
        var tile = Round.GetDiscard(Round.PreviousPlayerIndex)[Round.GetDiscard(Round.PreviousPlayerIndex).Count - 1];

        var hand = Round.GetHand(playerIndex);

        // pursuing kokushi or chiitoitsu requires staying fully concealed - never break it for a pon.
        if (hand.IsConcealed && PursuingClosedHandOnly(hand.ConcealedTiles, Round.DeadTilesFromIndexPointOfView(playerIndex)))
        {
            return false;
        }

        var tenpaiOpponentIndexes = GetTenpaiOpponentIndexes(playerIndex);

        // >= 66% of one family or honor
        var closeToHonitsuFamily = CloseToHonitsuFamily(hand.ConcealedTiles);

        // if the hand is already opened and no opponent tenpai: takes it
        if (!hand.IsConcealed && tenpaiOpponentIndexes.Count == 0 && (!closeToHonitsuFamily.HasValue || closeToHonitsuFamily == tile.Family))
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

        // pursuing kokushi or chiitoitsu requires staying fully concealed - never break it for a kan,
        // not even a concealed one: it still burns two tiles that could cover two other kokushi kinds,
        // and is outright illegal for chiitoitsu (no group of four identical tiles allowed).
        if (Round.GetHand(playerIndex).IsConcealed
            && PursuingClosedHandOnly(Round.GetHand(playerIndex).ConcealedTiles, Round.DeadTilesFromIndexPointOfView(playerIndex)))
        {
            return null;
        }

        var closeToHonitsuFamily = CloseToHonitsuFamily(Round.GetHand(playerIndex).ConcealedTiles);

        foreach (var tile in kanPossibilities)
        {
            // Call the kan if :
            // - it's a concealed one
            // - the hand is already open
            if (concealed
                || (!Round.GetHand(playerIndex).IsConcealed && (!closeToHonitsuFamily.HasValue || closeToHonitsuFamily == tile.Family)))
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
        var closeToHonitsuFamily = CloseToHonitsuFamily(Round.GetHand(Round.CurrentPlayerIndex).ConcealedTiles);

        if (Round.GetHand(Round.CurrentPlayerIndex).IsConcealed
            || tenpaiOppenentIndexes.Count > 0
            || (closeToHonitsuFamily.HasValue && closeToHonitsuFamily != chiiTiles[0].Family))
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

    // Whether the hand is close (>= 66%, honors included either way) to a single-family push
    // (honitsu/chinitsu), and if so, which family. Recomputed fresh from the current hand every
    // time it's needed (Pon/Kan/Chii calls and discard priority), rather than memoized once and
    // kept for the rest of the round: a hand that only looked honitsu-shaped for a moment (e.g. a
    // pile of unrelated honors that get discarded the very next turn) shouldn't stay "committed"
    // long after the tiles that triggered it are gone.
    private static Families? CloseToHonitsuFamily(IReadOnlyList<TilePivot> concealedTiles)
        => new Families?[] { Families.Bamboo, Families.Caracter, Families.Circle }
            .FirstOrDefault(f => concealedTiles.Count(t => t.Family == f || t.IsHonor) > 9);

    // Tiered "shape quality" for a tile value: a tile already locked into a complete run beats
    // everything else (breaking a finished group is always a step backwards), then an open-ended
    // ryanmen (e.g. 2-3, waits on two tile types), then an ordinary pair (backup pair / shanpon /
    // triplet potential), then a one-sided kanchan/penchan (e.g. 1-3 or 1-2, waits on a single tile
    // type), then an isolated tile with no shape at all.
    private int TaatsuQuality(TilePivot key, IReadOnlyList<TilePivot> concealedTiles)
    {
        var m2 = concealedTiles.Any(t => t.Family == key.Family && t.Number == key.Number - 2);
        var m1 = concealedTiles.Any(t => t.Family == key.Family && t.Number == key.Number - 1);
        var p1 = concealedTiles.Any(t => t.Family == key.Family && t.Number == key.Number + 1);
        var p2 = concealedTiles.Any(t => t.Family == key.Family && t.Number == key.Number + 2);

        // already the tail, middle or head of a complete run in hand - breaking it up would undo
        // finished work, so it outranks every still-incomplete shape below.
        if ((m2 && m1) || (m1 && p1) || (p1 && p2))
        {
            return 4;
        }

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

        // an isolated dora is still worth a turn or two of hope (it stays worth its bonus han
        // wherever it eventually lands), so it's not automatically the very worst tile to hold -
        // just as disposable as any other weak, unconnected shape, no more.
        var isDora = Round.GetDoraCount(key) > 0 || key.IsRedDora;
        return isDora ? 1 : 0;
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
        IReadOnlyList<(TilePivot tile, int unsafePoints)> tilesSafety, bool playsSafe, IReadOnlyList<TilePivot> deadTiles)
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

        if (PrefersLiveWait)
        {
            var allTileKinds = Round.FullTilesList.Distinct().ToList();
            orderedTiles = orderedTiles
                .ThenByDescending(t => WaitLiveTileCount(t, deadTiles, allTileKinds));
        }

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
                && deadTiles.Count(t => t == tile) == TilePivot.CopiesCount
                && deadTiles.GroupBy(t => t).Any(g => g.Key != tile && g.Key.IsHonorOrTerminal && g.Count() >= TilePivot.CopiesCount);

            // Kabe (wall): every one of a numbered tile's 4 copies already visible to us means nobody
            // holds or can draw one anymore - no shanpon, tanki, or either side of a ryanmen/kanchan/
            // penchan resolving specifically on this kind is possible, regardless of who's dangerous.
            var numberFullyDeadAndVisible = ReadsKabeForNumbers && !tile.IsHonor && deadTiles.Count(t => t == tile) == TilePivot.CopiesCount;

            foreach (var i in Enum.GetValues<PlayerIndices>().Where(i => i != Round.CurrentPlayerIndex))
            {
                // stop the building of the hand is opponent is riichi or has 3 or more combinations visible
                if (tenpaiOpponentIndexes.Contains(i))
                {
                    stopCurrentHand = true;
                    var summary = opponentSummaries[i];
                    if (summary.Tiles.Contains(tile) || honorIsolatedAndVisible || numberFullyDeadAndVisible)
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
            => number is >= TilePivot.MinNumber and <= TilePivot.MaxNumber && _numbersByFamily.TryGetValue(family, out var numbers) && numbers.Contains((byte)number);

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

    // Virtual so a variant (see NoDefenseCpuManagerPivot) can override "who looks dangerous" without
    // touching every call site that reacts to it (ComputeTilesSafety's stopCurrentHand, and the
    // defensive Pon/Kan/Chii declines below): they all funnel through this one method.
    protected virtual List<PlayerIndices> GetTenpaiOpponentIndexes(PlayerIndices playerIndex)
        => Enum.GetValues<PlayerIndices>().Where(i => i != playerIndex && PlayerIsCloseToWin(i)).ToList();

    // False here: a dangerous opponent only ever trims the discard choice toward safety (see
    // DiscardDecisionInternal), never gives up an already-reached tenpai. Overridden by
    // FullDefenseCpuManagerPivot to fold outright regardless of tenpai.
    protected virtual bool AbandonsHandEvenIfTenpai => false;

    // True here: among several choices that all keep (or reach) tenpai, this hand actively steers away
    // from ending its own turn in furiten - see PreferNonFuriten. Overridden by
    // BasicNoFuritenCpuManagerPivot to reproduce the original, furiten-blind behavior (kept only to
    // benchmark this fix's actual impact on win rate).
    protected virtual bool AvoidsFuriten => true;

    // True here: among several discard choices that all keep (or reach) tenpai with tied safety/dora,
    // this hand prefers the one leaving the wider and/or more live wait (e.g. a ryanmen whose tiles are
    // still mostly undiscarded) over a narrower one already mostly dead - see WaitLiveTileCount, used
    // as a tie-break in GetBestDiscardFromList. Overridden by BasicNoWaitWidthCpuManagerPivot to
    // reproduce the original behavior (kept only to benchmark this fix's actual impact on win rate).
    protected virtual bool PrefersLiveWait => true;

    // True here: ComputeTilesSafety additionally treats a numbered tile as guaranteed-safe once every
    // one of its 4 copies is already visible to us - nobody can be waiting on it (none left to hold or
    // draw), the same "kabe" (wall) reasoning already applied to isolated honors there (see
    // honorIsolatedAndVisible), just not gated on a second dead kind since kokushi musou - the reason
    // for that extra gate on honors - doesn't involve numbered tiles beyond 1/9. Overridden by
    // BasicNoNumberKabeCpuManagerPivot to reproduce the original behavior (kept only to benchmark this
    // fix's actual impact on win rate).
    protected virtual bool ReadsKabeForNumbers => true;

    // Whether discarding candidateDiscard would leave this player in (permanent) furiten: either an
    // earlier discard of theirs, or candidateDiscard itself (the classic "discard straight into your
    // own wait" trap), would complete the resulting hand. Deliberately checks only permanent furiten
    // (this player's own discard pile) - not temporary furiten (opponents' discards since this
    // player's last turn), which depends on turn order and resolves itself by this player's own next
    // discard anyway, for comparatively little gain against the added complexity.
    private bool WouldBeFuriten(PlayerIndices playerIndex, TilePivot candidateDiscard)
    {
        var pastAndCandidateDiscards = Round.GetDiscard(playerIndex).Append(candidateDiscard).ToList();
        return Round.GetHand(playerIndex).IsTenpai(pastAndCandidateDiscards, candidateDiscard);
    }

    // Prefers, among several discard choices that all keep (or reach) tenpai, the ones that don't
    // leave this player in furiten - falls back to the full list when every choice would be furiten
    // anyway (nothing to gain from filtering in that case, and the hand is no worse off either way).
    private IReadOnlyList<TilePivot> PreferNonFuriten(IReadOnlyList<TilePivot> choices)
    {
        if (!AvoidsFuriten)
        {
            return choices;
        }

        var nonFuriten = choices.Where(t => !WouldBeFuriten(Round.CurrentPlayerIndex, t)).ToList();
        return nonFuriten.Count > 0 ? nonFuriten : choices;
    }

    // Sums, across every kind of tile that would complete the hand once candidateDiscard is thrown,
    // how many copies are still live (not already visible in deadTiles) - out of 4 per kind. A wide
    // ryanmen with both sides fully live scores up to 8; a kanchan with its only winning kind already
    // 3/4 dead scores 1. Used as a tie-break: higher is a better wait to keep chasing.
    private int WaitLiveTileCount(TilePivot candidateDiscard, IReadOnlyList<TilePivot> deadTiles, IReadOnlyList<TilePivot> allTileKinds)
    {
        var waitTiles = Round.GetHand(Round.CurrentPlayerIndex).GetWaitTiles(allTileKinds, candidateDiscard);
        return waitTiles.Sum(w => TilePivot.CopiesCount - deadTiles.Count(d => d == w));
    }

    private bool PlayerIsCloseToWin(PlayerIndices i)
        => Round.IsRiichi(i) || Round.GetHand(i).DeclaredCombinations.Count > 2;

    #endregion Private methods
}
