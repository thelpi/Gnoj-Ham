using Gnoj_Ham_Library.Enums;

namespace Gnoj_Ham_Library;

/// <summary>
/// Pure combinatorics on tiles: whether a set of tiles forms a complete/tenpai hand, and how it
/// breaks down into combinations. Every method here is a function of its parameters only, with no
/// dependency on any particular <see cref="HandPivot"/> instance.
/// </summary>
internal static class TileCombinatoricsPivot
{
    private static readonly int[] ImpliesSingles = new[] { 1, 4, 7, 10 };
    private static readonly int[] ImpliesPairs = new[] { 2, 5, 8, 11 };
    private static readonly Families[] StandardFamilies = new[] { Families.Caracter, Families.Circle, Families.Bamboo };

    /// <summary>
    /// Checks if the specified list of tiles forms a complete hand while associated with an additional tile.
    /// </summary>
    /// <param name="tiles">List of tiles (other than <paramref name="declaredCombinations"/>); must be sorted (asc).</param>
    /// <param name="declaredCombinations">List of declared combinations.</param>
    /// <param name="additionalTile">The additional tile.</param>
    /// <param name="skipBasic"><c>True</c> to completely skip the basic check (example: two single winds).</param>
    /// <param name="recursiveCache">
    /// Optional; memoizes <see cref="GetCombinationSequencesRecursive"/> results by family and tile content.
    /// Only useful when calling this method repeatedly against the same base <paramref name="tiles"/> with a
    /// varying <paramref name="additionalTile"/> (see <see cref="IsTenpai"/>): three of the five family groups
    /// are then identical across calls and their (expensive) recursive decomposition can be reused as-is.
    /// <c>Null</c> (default) disables caching entirely.
    /// </param>
    /// <returns><c>True</c> if complete; <c>False</c> otherwise.</returns>
    internal static bool IsCompleteFull(IReadOnlyList<TilePivot> tiles,
        IReadOnlyList<TileComboPivot> declaredCombinations,
        TilePivot additionalTile,
        bool skipBasic = false,
        Dictionary<Families, (List<TilePivot> Tiles, List<List<TileComboPivot>> Result)>? recursiveCache = null)
    {
        var localCopy = new List<TilePivot>(tiles);
        localCopy.AddSorted(additionalTile);
        return (!skipBasic && IsCompleteBasic(localCopy, declaredCombinations.Count, recursiveCache))
            || IsSevenPairs(localCopy)
            || IsThirteenOrphans(localCopy);
    }

    /// <summary>
    /// Checks if the hand contains a valuable pair (dragon, dominant wind, player wind).
    /// </summary>
    /// <param name="combinations">Lsit of combinations.</param>
    /// <param name="dominantWind">The dominant wind.</param>
    /// <param name="playerWind">The player wind.</param>
    /// <returns><c>True</c> if vluable pair in the hand; <c>False</c> otherwise.</returns>
    internal static bool HandWithValuablePair(IReadOnlyList<TileComboPivot> combinations, Winds dominantWind, Winds playerWind)
    {
        return combinations.Any(c => c.IsPair && (
            c.Family == Families.Dragon
            || (c.Family == Families.Wind && (c.Tiles[0].Wind == dominantWind || c.Tiles[0].Wind == playerWind))
        ));
    }

    /// <summary>
    /// Checks if the specified tiles form a valid "Kokushi musou" (thirteen orphans); tiles have to be sorted.
    /// </summary>
    /// <param name="tiles">List of tiles.</param>
    /// <returns><c>True</c> if "Kokushi musou"; <c>False</c> otherwise.</returns>
    internal static bool IsThirteenOrphans(IReadOnlyList<TilePivot> tiles)
    {
        if (tiles.Count != HandPivot.FullSize)
        {
            return false;
        }

        TilePivot? tile = null;
        var i = 0;
        var paired = false;
        foreach (var t in tiles)
        {
            if (!t.IsHonorOrTerminal)
            {
                return false;
            }

            if (tile == null || tile != t)
            {
                tile = t;
                i = 1;
            }
            else if (i == 2 || paired)
            {
                return false;
            }
            else
            {
                i++;
                paired = true;
            }
        }

        return paired;
    }

    /// <summary>
    /// Checks if the specified tiles form a valid "Chiitoitsu" (seven pairs); tiles have to be sorted.
    /// </summary>
    /// <param name="tiles">List of tiles.</param>
    /// <returns><c>True</c> if "Chiitoitsu"; <c>False</c> otherwise.</returns>
    internal static bool IsSevenPairs(IReadOnlyList<TilePivot> tiles)
    {
        if (tiles.Count != HandPivot.FullSize)
        {
            return false;
        }

        TilePivot? tile = null;
        var i = 0;
        foreach (var t in tiles)
        {
            if (tile == null)
            {
                tile = t;
                i = 1;
            }
            else if (t == tile)
            {
                if (i == 2)
                {
                    return false;
                }
                i++;
            }
            else if (i < 2)
            {
                return false;
            }
            else
            {
                tile = t;
                i = 1;
            }
        }

        return true;
    }

    /// <summary>
    /// Checks if the specified tiles form a complete hand (four combinations of three tiles and a pair).
    /// "Kokushi musou" and "Chiitoitsu" must be checked separately.
    /// </summary>
    /// <param name="concealedTiles">List of concealed tiles.</param>
    /// <param name="declaredCombinationsCount">Count of declared combinations.</param>
    /// <param name="recursiveCache">Optional; see <see cref="IsCompleteFull"/>.</param>
    /// <returns>True if the hand is complete.</returns>
    internal static bool IsCompleteBasic(IReadOnlyList<TilePivot> concealedTiles, int declaredCombinationsCount,
        Dictionary<Families, (List<TilePivot> Tiles, List<List<TileComboPivot>> Result)>? recursiveCache = null)
    {
        // Every combinations are declared.
        if (declaredCombinationsCount == HandPivot.MeldsCount)
        {
            // The last two should form a pair.
            return concealedTiles[0] == concealedTiles[1];
        }

        var combinationsSequences = GetCombinationsSequences(concealedTiles, declaredCombinationsCount, out var forceExit, recursiveCache);

        return forceExit || combinationsSequences.Any(cs => CombinationSequenceIsValid(declaredCombinationsCount, cs));
    }

    private static bool CombinationSequenceIsValid(int declaredCombinationsCount, List<TileComboPivot> cs)
    {
        if (cs.Count != HandPivot.MeldsCount + 1 - declaredCombinationsCount)
        {
            return false;
        }

        var paired = false;
        foreach (var c in cs)
        {
            if (c.IsPair)
            {
                if (paired)
                    return false;
                paired = true;
            }
        }

        return paired;
    }

    /// <summary>
    /// Checks if the specified tiles form a complete hand (four combinations of three tiles and a pair).
    /// "Kokushi musou" and "Chiitoitsu" must be checked separately.
    /// </summary>
    /// <param name="concealedTiles">List of concealed tiles; have to be sorted.</param>
    /// <param name="declaredCombinations">List of declared combinations.</param>
    /// <returns>A list of every valid sequences of combinations.</returns>
    internal static IReadOnlyList<List<TileComboPivot>> IsCompleteBasic(IReadOnlyList<TilePivot> concealedTiles, IReadOnlyList<TileComboPivot> declaredCombinations)
    {
        // Every combinations are declared.
        if (declaredCombinations.Count == HandPivot.MeldsCount)
        {
            return concealedTiles[0] == concealedTiles[1]
                ? new List<List<TileComboPivot>>
                {
                    new(declaredCombinations)
                    {
                        new TileComboPivot(concealedTiles)
                    }
                }
                : new List<List<TileComboPivot>>();
        }

        var combinationsSequences = GetCombinationsSequences(concealedTiles, -1, out _);

        // Adds the declared combinations to each sequence of combinations.
        foreach (var combinationsSequence in combinationsSequences)
        {
            combinationsSequence.AddRange(declaredCombinations);
        }

        // Filters invalid sequences :
        // - Doesn't contain exactly 5 combinations.
        // - Doesn't contain a pair.
        // - Contains more than one pair.
        combinationsSequences.RemoveAll(cs => !CombinationSequenceIsValid(0, cs));

        // Filters duplicates sequences
        combinationsSequences.RemoveAll(cs1 =>
            combinationsSequences.Exists(cs2 =>
                combinationsSequences.IndexOf(cs2) < combinationsSequences.IndexOf(cs1)
                && cs1.IsBijection(cs2)));

        return combinationsSequences;
    }

    /// <summary>
    /// Computes if a hand is tenpai (any of <paramref name="notInHandTiles"/> can complete the hand, which must have 13th tiles).
    /// </summary>
    /// <param name="concealedTiles">Concealed tiles of the hand.</param>
    /// <param name="combinations">Declared combinations of the hand.</param>
    /// <param name="notInHandTiles">List of substitution tiles.</param>
    /// <param name="skipBasic"><c>True</c> to skip regular tenpai check (example : two single winds).</param>
    /// <returns><c>True</c> if tenpai; <c>False</c> otherwise.</returns>
    internal static bool IsTenpai(IReadOnlyList<TilePivot> concealedTiles,
        IReadOnlyList<TileComboPivot> combinations,
        IReadOnlyList<TilePivot> notInHandTiles,
        bool skipBasic)
    {
        // concealedTiles is fixed across every substitution tile tried below: at most one family group
        // actually changes per attempt (whichever one the substitution tile belongs to), so the other
        // four can have their (expensive) recursive decomposition computed once and reused instead of
        // recomputed from scratch for every single one of the notInHandTiles candidates.
        var recursiveCache = new Dictionary<Families, (List<TilePivot> Tiles, List<List<TileComboPivot>> Result)>();

        foreach (var sub in notInHandTiles)
        {
            if (IsCompleteFull(concealedTiles, combinations, sub, skipBasic, recursiveCache))
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Computes the actual wait: every tile from <paramref name="candidateTiles"/> that would complete
    /// the hand (which must have 13 tiles), unlike <see cref="IsTenpai"/> which only checks if any does.
    /// </summary>
    /// <param name="concealedTiles">Concealed tiles of the hand.</param>
    /// <param name="combinations">Declared combinations of the hand.</param>
    /// <param name="candidateTiles">List of candidate tiles (one per distinct kind is enough).</param>
    /// <returns>The subset of <paramref name="candidateTiles"/> that would complete the hand.</returns>
    internal static List<TilePivot> GetWaitTiles(IReadOnlyList<TilePivot> concealedTiles,
        IReadOnlyList<TileComboPivot> combinations,
        IReadOnlyList<TilePivot> candidateTiles)
    {
        var recursiveCache = new Dictionary<Families, (List<TilePivot> Tiles, List<List<TileComboPivot>> Result)>();

        return candidateTiles.Where(sub => IsCompleteFull(concealedTiles, combinations, sub, false, recursiveCache)).ToList();
    }

    // Gets every possible combinations from the given list of tiles
    // declaredCombinationsCount => -1 to not exit at first
    // concealedTiles have to be sorted
    private static List<List<TileComboPivot>> GetCombinationsSequences(
        IReadOnlyList<TilePivot> concealedTiles,
        int declaredCombinationsCount,
        out bool forceExit,
        Dictionary<Families, (List<TilePivot> Tiles, List<List<TileComboPivot>> Result)>? recursiveCache = null)
    {
        forceExit = false;

        // bad approximation of size
        var combinationsSequences = new List<List<TileComboPivot>>(20);

        var pairCount = 0;
        Families? family = null;
        var familyGroups = new Dictionary<Families, List<TilePivot>>
        {
            { Families.Dragon, new(5) },
            { Families.Wind, new(5) },
            { Families.Caracter, new(5) },
            { Families.Circle, new(5) },
            { Families.Bamboo, new(5) },
        };
        var i = 0;
        foreach (var tile in concealedTiles)
        {
            if (!family.HasValue)
            {
                family = tile.Family;
            }
            else if (family.Value != tile.Family)
            {
                // The first case is not possible because its implies a single tile or several pairs.
                if (ImpliesSingles.Contains(familyGroups[family.Value].Count))
                {
                    return combinationsSequences;
                }
                // The second case is not possible more than once because its implies a pair.
                if (ImpliesPairs.Contains(familyGroups[family.Value].Count))
                {
                    pairCount++;
                    if (pairCount > 1)
                        return combinationsSequences;
                }
                family = tile.Family;
            }
            familyGroups[family.Value].Add(tile);
            i++;
            if (i == concealedTiles.Count)
            {
                if (ImpliesSingles.Contains(familyGroups[family.Value].Count))
                {
                    return combinationsSequences;
                }
                if (ImpliesPairs.Contains(familyGroups[family.Value].Count))
                {
                    pairCount++;
                    if (pairCount > 1)
                        return combinationsSequences;
                }
            }
        }

        if (familyGroups[Families.Dragon].Count > 0)
        {
            var dragonCombinations = GetHonorCombinations(familyGroups[Families.Dragon], t => t.Dragon!.Value);
            if (dragonCombinations.Count == 0)
            {
                return new List<List<TileComboPivot>>();
            }
            combinationsSequences.Add(dragonCombinations);
        }

        if (familyGroups[Families.Wind].Count > 0)
        {
            var windCombinations = GetHonorCombinations(familyGroups[Families.Wind], t => t.Wind!.Value);
            if (windCombinations.Count == 0)
            {
                return new List<List<TileComboPivot>>();
            }
            if (combinationsSequences.Count > 0)
            {
                foreach (var cs in combinationsSequences)
                    cs.AddRange(windCombinations);
            }
            else
            {
                combinationsSequences.Add(windCombinations);
            }
        }

        foreach (var oneFamily in StandardFamilies)
        {
            if (familyGroups[oneFamily].Count > 0)
            {
                var temporaryCombinationsSequences = GetCachedCombinationSequencesRecursive(oneFamily, familyGroups[oneFamily], recursiveCache);
                if (combinationsSequences.Count > 0)
                {
                    // Cartesian product of existant sequences and temporary list.
                    var newCombinationsSequences = new List<List<TileComboPivot>>(combinationsSequences.Count * temporaryCombinationsSequences.Count);
                    foreach (var cs in combinationsSequences)
                    {
                        foreach (var cs2 in temporaryCombinationsSequences)
                        {
                            var x = new List<TileComboPivot>(cs);
                            x.AddRange(cs2);
                            newCombinationsSequences.Add(new List<TileComboPivot>(x));
                            if (declaredCombinationsCount > -1 && CombinationSequenceIsValid(declaredCombinationsCount, newCombinationsSequences[^1]))
                            {
                                forceExit = true;
                                return newCombinationsSequences;
                            }
                        }
                    }
                    combinationsSequences = newCombinationsSequences;
                }
                else
                {
                    combinationsSequences = temporaryCombinationsSequences;
                }
            }
        }

        return combinationsSequences;
    }

    private static List<TileComboPivot> GetHonorCombinations<T>(List<TilePivot> tiles, Func<TilePivot, T> getValue)
        where T : struct
    {
        T? currentV = null;
        var count = 0;
        var j = 0;
        var combinations = new List<TileComboPivot>(4);
        foreach (var t in tiles)
        {
            if (!currentV.HasValue || !currentV.Value.Equals(getValue(t)))
            {
                if (count == 2)
                {
                    combinations.Add(new TileComboPivot(tiles[j - 2], tiles[j - 1]));
                }
                else if (count == 3)
                {
                    combinations.Add(new TileComboPivot(tiles[j - 3], tiles[j - 2], tiles[j - 1]));
                }
                else if (count == 5)
                {
                    // Only reachable when probing tenpai on a hand that already holds all four copies
                    // of its own wait: every tile value is tried as a candidate completion regardless
                    // of how many are already in hand, so this run momentarily represents five copies
                    // of the same honor - split as a triplet plus the completing pair (never a real,
                    // standing hand: at most four copies of a tile exist).
                    combinations.Add(new TileComboPivot(tiles[j - 5], tiles[j - 4], tiles[j - 3]));
                    combinations.Add(new TileComboPivot(tiles[j - 2], tiles[j - 1]));
                }
                currentV = getValue(t);
                count = 1;
            }
            else
            {
                count++;
            }
            j++;
            if (j == tiles.Count)
            {
                if (count == 2)
                {
                    combinations.Add(new TileComboPivot(tiles[j - 2], tiles[j - 1]));
                }
                else if (count == 3)
                {
                    combinations.Add(new TileComboPivot(tiles[j - 3], tiles[j - 2], tiles[j - 1]));
                }
                else if (count == 5)
                {
                    combinations.Add(new TileComboPivot(tiles[j - 5], tiles[j - 4], tiles[j - 3]));
                    combinations.Add(new TileComboPivot(tiles[j - 2], tiles[j - 1]));
                }
            }
        }
        return combinations;
    }

    // Assumes that all tiles are from the same family, and this family is caracter / circle / bamboo.
    // Also assumes that referenced tile is included in the list.
    private static List<TileComboPivot> GetCombinationsForTile(TilePivot tile, List<TilePivot> tiles)
    {
        var combinations = new List<TileComboPivot>(5);

        TilePivot? secondLow = null;
        TilePivot? firstLow = null;
        TilePivot? firstHigh = null;
        TilePivot? secondHigh = null;
        var count = 0;
        foreach (var t in tiles)
        {
            if (t.Number == tile.Number)
            {
                count++;
                if (count == 2)
                {
                    combinations.Add(new TileComboPivot(tile, tile));
                }
                else if (count == 3)
                {
                    combinations.Add(new TileComboPivot(tile, tile, tile));
                }
            }
            else if (t.Number == tile.Number - 2)
            {
                secondLow = t;
            }
            else if (t.Number == tile.Number - 1)
            {
                firstLow = t;
            }
            else if(t.Number == tile.Number + 1)
            {
                firstHigh = t;
            }
            else if (t.Number == tile.Number + 2)
            {
                secondHigh = t;
            }
        }

        if (secondLow != null && firstLow != null)
        {
            // Can make a sequence.
            combinations.Add(new TileComboPivot(secondLow, firstLow, tile));
        }
        if (firstLow != null && firstHigh != null)
        {
            // Can make a sequence.
            combinations.Add(new TileComboPivot(firstLow, tile, firstHigh));
        }
        if (firstHigh != null && secondHigh != null)
        {
            // Can make a sequence.
            combinations.Add(new TileComboPivot(tile, firstHigh, secondHigh));
        }

        return combinations;
    }

    // Same as GetCombinationSequencesRecursive, memoized by (family, tile content) when a cache is provided.
    // Single-slot-per-family cache: within one IsTenpai call, the tile list for a family untouched by
    // the current substitution tile is rebuilt from the exact same TilePivot references, in the exact
    // same order, every time - so a cheap reference-by-reference comparison against the last computed
    // input is enough to detect a hit, with no hashing or string allocation needed.
    private static List<List<TileComboPivot>> GetCachedCombinationSequencesRecursive(
        Families family, List<TilePivot> tiles, Dictionary<Families, (List<TilePivot> Tiles, List<List<TileComboPivot>> Result)>? cache)
    {
        if (cache == null)
        {
            return GetCombinationSequencesRecursive(tiles);
        }

        if (cache.TryGetValue(family, out var cached) && SameTiles(cached.Tiles, tiles))
        {
            return cached.Result;
        }

        var result = GetCombinationSequencesRecursive(tiles);
        cache[family] = (tiles, result);
        return result;
    }

    private static bool SameTiles(List<TilePivot> a, List<TilePivot> b)
    {
        if (a.Count != b.Count)
        {
            return false;
        }

        for (var i = 0; i < a.Count; i++)
        {
            if (!ReferenceEquals(a[i], b[i]))
            {
                return false;
            }
        }

        return true;
    }

    // Assumes that all tiles are from the same family, and this family is caracter / circle / bamboo.
    private static List<List<TileComboPivot>> GetCombinationSequencesRecursive(List<TilePivot> tiles)
    {
        var combinationsSequences = new List<List<TileComboPivot>>(10);

        if (ImpliesSingles.Contains(tiles.Count))
        {
            return combinationsSequences;
        }

        if (tiles.Count == 2)
        {
            // aa
            if (tiles[0] == tiles[1])
            {
                combinationsSequences.Add(new List<TileComboPivot> { new(tiles[0], tiles[1]) });
            }
            return combinationsSequences;
        }

        if (tiles.Count == 3)
        {
            // aaa
            // abc
            if ((tiles[0] == tiles[1] && tiles[0] == tiles[2])
                || (tiles[0].Number == tiles[1].Number - 1 && tiles[0].Number == tiles[2].Number - 2))
            {
                combinationsSequences.Add(new List<TileComboPivot> { new(tiles[0], tiles[1], tiles[2]) });
            }
            return combinationsSequences;
        }

        if (tiles.Count == 5)
        {
            // aaa bb
            // abc cc
            if ((tiles[0] == tiles[1]
                    && tiles[0] == tiles[2]
                    && tiles[3] == tiles[4])
                || (tiles[0].Number == tiles[1].Number - 1
                    && tiles[0].Number == tiles[2].Number - 2
                    && tiles[3] == tiles[4]))
            {
                combinationsSequences.Add(new List<TileComboPivot> { new(tiles[0], tiles[1], tiles[2]), new(tiles[3], tiles[4]) });
            }
            // aa abc
            // aa bbb
            else if ((tiles[0] == tiles[1]
                    && tiles[2].Number == tiles[3].Number - 1
                    && tiles[2].Number == tiles[4].Number - 2)
                || (tiles[0] == tiles[1]
                    && tiles[2] == tiles[3]
                    && tiles[3] == tiles[4]))
            {
                combinationsSequences.Add(new List<TileComboPivot> { new(tiles[0], tiles[1]), new(tiles[2], tiles[3], tiles[4]) });
            }
            // abbbc
            else if (tiles[0].Number == tiles[1].Number - 1
                && tiles[0].Number == tiles[4].Number - 2
                && tiles[1] == tiles[2]
                && tiles[1] == tiles[3])
            {
                combinationsSequences.Add(new List<TileComboPivot> { new(tiles[0], tiles[1], tiles[4]), new(tiles[2], tiles[3]) });
            }
            return combinationsSequences;
        }

        if (tiles.Count == 6)
        {
            // aaabbb
            if (tiles[0] == tiles[1]
                && tiles[0] == tiles[2]
                && tiles[3] == tiles[4]
                && tiles[3] == tiles[5])
            {
                combinationsSequences.Add(new List<TileComboPivot> { new(tiles[0], tiles[1], tiles[2]), new(tiles[3], tiles[4], tiles[5]) });
            }
            // aaaabc
            else if (tiles[0] == tiles[1]
                && tiles[0] == tiles[2]
                && tiles[3].Number == tiles[4].Number - 1
                && tiles[3].Number == tiles[5].Number - 2)
            {
                combinationsSequences.Add(new List<TileComboPivot> { new(tiles[0], tiles[1], tiles[2]), new(tiles[3], tiles[4], tiles[5]) });
            }
            // aabbcc
            else if (tiles[0] == tiles[1]
                && tiles[2] == tiles[3]
                && tiles[4] == tiles[5]
                && tiles[0].Number == tiles[2].Number - 1
                && tiles[0].Number == tiles[4].Number - 2)
            {
                combinationsSequences.Add(new List<TileComboPivot> { new(tiles[0], tiles[2], tiles[4]), new(tiles[1], tiles[3], tiles[5]) });
            }
            // abbbbc
            else if (tiles[0].Number == tiles[1].Number - 1
                && tiles[0].Number == tiles[5].Number - 2
                && tiles[2] == tiles[3]
                && tiles[2] == tiles[4])
            {
                combinationsSequences.Add(new List<TileComboPivot> { new(tiles[0], tiles[1], tiles[5]), new(tiles[2], tiles[3], tiles[4]) });
            }
            // abcccc
            else if (tiles[0].Number == tiles[1].Number - 1
                && tiles[0].Number == tiles[2].Number - 2
                && tiles[3] == tiles[4]
                && tiles[3] == tiles[5])
            {
                combinationsSequences.Add(new List<TileComboPivot> { new(tiles[0], tiles[1], tiles[2]), new(tiles[3], tiles[4], tiles[5]) });
            }
            // abccdf
            else if (tiles[0].Number == tiles[1].Number - 1
                && tiles[0].Number == tiles[2].Number - 2
                && tiles[3].Number == tiles[4].Number - 1
                && tiles[3].Number == tiles[5].Number - 2)
            {
                combinationsSequences.Add(new List<TileComboPivot> { new(tiles[0], tiles[1], tiles[2]), new(tiles[3], tiles[4], tiles[5]) });
            }
            // abbccd
            if (tiles[0].Number == tiles[1].Number - 1
                && tiles[0].Number == tiles[3].Number - 2
                && tiles[2].Number == tiles[4].Number - 1
                && tiles[2].Number == tiles[5].Number - 2)
            {
                combinationsSequences.Add(new List<TileComboPivot> { new(tiles[0], tiles[1], tiles[3]), new(tiles[2], tiles[4], tiles[5]) });
            }
            return combinationsSequences;
        }

        var checkedNumbers = new List<int>(tiles.Count);
        foreach (var currentTile in tiles)
        {
            var number = currentTile.Number;
            if (checkedNumbers.Contains(number))
                continue;
            checkedNumbers.Add(number);

            var combinations = GetCombinationsForTile(currentTile, tiles);
            foreach (var combination in combinations)
            {
                var subTiles = new List<TilePivot>(tiles);
                foreach (var tile in combination.Tiles)
                {
                    subTiles.Remove(tile);
                }
                if (subTiles.Count > 0)
                {
                    var subCombinationsSequences = GetCombinationSequencesRecursive(subTiles);
                    foreach (var combinationsSequence in subCombinationsSequences)
                    {
                        combinationsSequence.Add(combination);
                        combinationsSequences.Add(combinationsSequence);
                    }
                }
                else
                {
                    combinationsSequences.Add(new List<TileComboPivot> { combination });
                }
            }
        }

        return combinationsSequences;
    }
}
