namespace Gnoj_Ham_Library;

/// <summary>
/// The shanten number of a hand: the fewest tiles that have to be exchanged (drawn, then discarded) to
/// make it tenpai. <c>-1</c> is a complete hand, <c>0</c> a tenpai one, <c>1</c> an iishanten one, and so on.
/// Worked out directly from the shape of the hand, with no need to try every tile that could be drawn.
/// Like <see cref="TileCombinatoricsPivot"/>, a function of its parameters only; it looks at the shape only
/// (not at the yakus, nor at the copies of a tile that are left to draw).
/// </summary>
internal static class ShantenCalculatorPivot
{
    // A meld is worth two steps of the way to a complete hand, and so is a partial group (two tiles
    // waiting for a third) or the pair, one: the shanten of a hand with none of them.
    private const int NothingShanten = 2 * HandPivot.MeldsCount;

    private const int SevenPairsCount = HandPivot.FullSize / TileComboPivot.PairSize;

    // Every terminal and every honor, once, for a thirteen orphans.
    private static readonly int ThirteenOrphansKindsCount = (TilePivot.SuitsCount * 2) + TilePivot.HonorKindsCount;

    // The melds a group of kinds can hold, from none to all of them, with or without the pair.
    private const int FrontierSize = (HandPivot.MeldsCount + 1) * 2;

    // Nothing can be made of the group with this many melds (and this pair, or none).
    private const int Unreachable = -1;

    // Kinds that can't be combined with each other are searched apart: each suit, and the honors.
    private static readonly KindsGroup[] Groups = BuildGroups();

    /// <summary>
    /// Works out the shanten number of a hand.
    /// </summary>
    /// <param name="concealedTiles">The concealed tiles of the hand: <c>13</c> minus the ones in the declared combinations, or one more.</param>
    /// <param name="declaredCombinationsCount">The count of declared combinations.</param>
    /// <returns>The shanten number.</returns>
    internal static int Compute(IReadOnlyList<TilePivot> concealedTiles, int declaredCombinationsCount)
    {
        var kindCounts = new int[TilePivot.KindsCount];
        foreach (var tile in concealedTiles)
        {
            kindCounts[tile.KindIndex]++;
        }

        return ComputeInPlace(kindCounts, declaredCombinationsCount);
    }

    /// <summary>
    /// Works out the shanten number of a hand.
    /// </summary>
    /// <param name="kindCounts">
    /// How many concealed tiles the hand holds of each kind (see <see cref="TilePivot.KindIndex"/>);
    /// left as it is.
    /// </param>
    /// <param name="declaredCombinationsCount">The count of declared combinations.</param>
    /// <returns>The shanten number.</returns>
    internal static int Compute(int[] kindCounts, int declaredCombinationsCount)
    {
        return ComputeInPlace((int[])kindCounts.Clone(), declaredCombinationsCount);
    }

    // The search puts tiles aside and takes them back: it works on a copy nobody else holds.
    private static int ComputeInPlace(int[] kindCounts, int declaredCombinationsCount)
    {
        var shanten = RegularHandShanten(kindCounts, declaredCombinationsCount);

        // Seven pairs and thirteen orphans are for a hand with nothing declared.
        if (declaredCombinationsCount == 0)
        {
            shanten = Math.Min(shanten, Math.Min(SevenPairsShanten(kindCounts), ThirteenOrphansShanten(kindCounts)));
        }

        return shanten;
    }

    // Four melds and a pair: each group of kinds says how many partial groups it can hold along with a
    // number of melds (and the pair), and the groups are put together keeping the best of each.
    private static int RegularHandShanten(int[] kindCounts, int declaredCombinationsCount)
    {
        var partials = NewFrontier();
        partials[declaredCombinationsCount * 2] = 0;

        foreach (var group in Groups)
        {
            if (group.HoldsTiles(kindCounts))
            {
                partials = Combine(partials, new GroupSearch(kindCounts, group).Run());
            }
        }

        var shanten = int.MaxValue;
        for (var slot = 0; slot < FrontierSize; slot++)
        {
            if (partials[slot] == Unreachable)
            {
                continue;
            }

            var melds = slot / 2;
            var hasPair = slot % 2;

            // Only so many groups are needed to make a hand: the partial groups beyond them are of no use.
            var usefulPartials = Math.Min(partials[slot], HandPivot.MeldsCount - melds);
            shanten = Math.Min(shanten, NothingShanten - (2 * melds) - usefulPartials - hasPair);
        }

        return shanten;
    }

    // Seven different pairs: a kind held more than twice is still just one pair.
    private static int SevenPairsShanten(int[] kindCounts)
    {
        var pairs = kindCounts.Count(c => c >= TileComboPivot.PairSize);
        var kinds = kindCounts.Count(c => c > 0);

        return (SevenPairsCount - 1 - pairs) + Math.Max(0, SevenPairsCount - kinds);
    }

    // Every terminal and honor, and a pair of any of them.
    private static int ThirteenOrphansShanten(int[] kindCounts)
    {
        var kinds = 0;
        var hasPair = false;
        for (var kind = 0; kind < kindCounts.Length; kind++)
        {
            if (kindCounts[kind] > 0 && IsTerminalOrHonor(kind))
            {
                kinds++;
                hasPair |= kindCounts[kind] >= TileComboPivot.PairSize;
            }
        }

        return ThirteenOrphansKindsCount - kinds - (hasPair ? 1 : 0);
    }

    private static bool IsTerminalOrHonor(int kind)
    {
        if (kind >= TilePivot.SuitsCount * TilePivot.MaxNumber)
        {
            return true;
        }

        var number = (kind % TilePivot.MaxNumber) + TilePivot.MinNumber;
        return number is TilePivot.MinNumber or TilePivot.MaxNumber;
    }

    // Slot of a frontier: two for each count of melds, the second one being for "with the pair".
    private static int[] NewFrontier()
    {
        var frontier = new int[FrontierSize];
        Array.Fill(frontier, Unreachable);
        return frontier;
    }

    // What two sets of groups can make together: the melds add up, and there can be one pair at most.
    private static int[] Combine(int[] first, int[] second)
    {
        var combined = NewFrontier();
        for (var slot1 = 0; slot1 < FrontierSize; slot1++)
        {
            for (var slot2 = 0; slot2 < FrontierSize; slot2++)
            {
                if (first[slot1] == Unreachable || second[slot2] == Unreachable)
                {
                    continue;
                }

                var melds = (slot1 / 2) + (slot2 / 2);
                var pairs = (slot1 % 2) + (slot2 % 2);
                if (melds > HandPivot.MeldsCount || pairs > 1)
                {
                    continue;
                }

                var slot = (melds * 2) + pairs;
                combined[slot] = Math.Max(combined[slot], first[slot1] + second[slot2]);
            }
        }

        return combined;
    }

    private static KindsGroup[] BuildGroups()
    {
        var groups = new List<KindsGroup>();
        for (var suit = 0; suit < TilePivot.SuitsCount; suit++)
        {
            groups.Add(new KindsGroup(suit * TilePivot.MaxNumber, TilePivot.MaxNumber, true));
        }

        groups.Add(new KindsGroup(TilePivot.SuitsCount * TilePivot.MaxNumber, TilePivot.HonorKindsCount, false));
        return groups.ToArray();
    }

    // A run of kinds: a suit (where kinds next to each other make sequences) or the honors (where they don't).
    private readonly record struct KindsGroup(int Start, int Length, bool IsSuit)
    {
        internal bool HoldsTiles(int[] kindCounts)
        {
            for (var kind = Start; kind < Start + Length; kind++)
            {
                if (kindCounts[kind] > 0)
                {
                    return true;
                }
            }

            return false;
        }
    }

    // Tries every way to read the tiles of a group as melds, partial groups, a pair and tiles left alone.
    private sealed class GroupSearch
    {
        private readonly int[] _counts;
        private readonly KindsGroup _group;
        private readonly int[] _frontier = NewFrontier();

        private int _melds;
        private int _partials;
        private bool _hasPair;

        internal GroupSearch(int[] kindCounts, KindsGroup group)
        {
            _counts = kindCounts;
            _group = group;
        }

        // For each count of melds, with or without the pair: the most partial groups the tiles can also make.
        internal int[] Run()
        {
            Search(_group.Start);
            return _frontier;
        }

        private void Search(int kind)
        {
            var end = _group.Start + _group.Length;
            while (kind < end && _counts[kind] == 0)
            {
                kind++;
            }

            if (kind == end)
            {
                var slot = (_melds * 2) + (_hasPair ? 1 : 0);
                _frontier[slot] = Math.Max(_frontier[slot], _partials);
                return;
            }

            // The room left in the group after this kind, for what has to lie next to it.
            var kindsAfter = end - kind - 1;

            if (_counts[kind] >= TileComboPivot.MeldSize)
            {
                _counts[kind] -= TileComboPivot.MeldSize;
                _melds++;
                Search(kind);
                _melds--;
                _counts[kind] += TileComboPivot.MeldSize;
            }

            if (_group.IsSuit && kindsAfter >= 2 && _counts[kind + 1] > 0 && _counts[kind + 2] > 0)
            {
                _counts[kind]--;
                _counts[kind + 1]--;
                _counts[kind + 2]--;
                _melds++;
                Search(kind);
                _melds--;
                _counts[kind]++;
                _counts[kind + 1]++;
                _counts[kind + 2]++;
            }

            if (_counts[kind] >= TileComboPivot.PairSize)
            {
                _counts[kind] -= TileComboPivot.PairSize;

                // The pair of the hand...
                if (!_hasPair)
                {
                    _hasPair = true;
                    Search(kind);
                    _hasPair = false;
                }

                // ...or two tiles waiting for a third.
                _partials++;
                Search(kind);
                _partials--;

                _counts[kind] += TileComboPivot.PairSize;
            }

            // Two tiles of a sequence: next to each other, or with one between them.
            if (_group.IsSuit && kindsAfter >= 1 && _counts[kind + 1] > 0)
            {
                SearchPartial(kind, kind + 1);
            }

            if (_group.IsSuit && kindsAfter >= 2 && _counts[kind + 2] > 0)
            {
                SearchPartial(kind, kind + 2);
            }


            // The tile stays alone.
            _counts[kind]--;
            Search(kind);
            _counts[kind]++;
        }

        private void SearchPartial(int kind, int otherKind)
        {
            _counts[kind]--;
            _counts[otherKind]--;
            _partials++;
            Search(kind);
            _partials--;
            _counts[kind]++;
            _counts[otherKind]++;
        }
    }
}
