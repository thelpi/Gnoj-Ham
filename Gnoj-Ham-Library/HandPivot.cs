using Gnoj_Ham_Library.Enums;

namespace Gnoj_Ham_Library;

/// <summary>
/// Represents an hand.
/// </summary>
public class HandPivot
{
    #region Embedded properties

    private readonly List<TilePivot> _concealedTiles;
    private readonly List<TileComboPivot> _declaredCombinations;

    /// <summary>
    /// List of concealed tiles.
    /// </summary>
    public IReadOnlyList<TilePivot> ConcealedTiles => _concealedTiles;
    /// <summary>
    /// List of declared <see cref="TileComboPivot"/>.
    /// </summary>
    public IReadOnlyList<TileComboPivot> DeclaredCombinations => _declaredCombinations;

    /// <summary>
    /// The latest pick (from wall or steal); can't be known by <see cref="_concealedTiles"/> (sorted list).
    /// </summary>
    internal TilePivot LatestPick { get; private set; }

    /// <summary>
    /// Yakus, if the hand is complete; otherwise <c>Null</c>.
    /// </summary>
    internal IReadOnlyList<YakuPivot>? Yakus { get; private set; }

    /// <summary>
    /// Combinations computed in the hand to produce <see cref="Yakus"/>;
    /// <c>Null</c> if <see cref="Yakus"/> is <c>Null</c> or contains <see cref="YakuPivot.KokushiMusou"/> or <see cref="YakuPivot.NagashiMangan"/>.
    /// </summary>
    internal IReadOnlyList<TileComboPivot>? YakusCombinations { get; private set; }

    #endregion Embedded properties

    #region Inferred properties

    /// <summary>
    /// Inferred; indicates if the hand is complete (can tsumo or ron depending on context).
    /// </summary>
    internal bool IsComplete => Yakus != null && Yakus.Count > 0;

    /// <summary>
    /// Inferred; indicates if the hand is concealed.
    /// </summary>
    internal bool IsConcealed => !_declaredCombinations.Any(c => !c.IsConcealed);

    /// <summary>
    /// Inferred; every tiles of the hand; concealed or not; into combination or not.
    /// </summary>
    internal IReadOnlyList<TilePivot> AllTiles
    {
        get
        {
            var allTiles = _declaredCombinations.SelectMany(t => t.Tiles).Concat(_concealedTiles).ToList();
            if (LatestPick != null && !allTiles.Any(t => ReferenceEquals(t, LatestPick)))
            {
                allTiles.Add(LatestPick);
            }

            return allTiles;
        }
    }

    /// <summary>
    /// Inferred; indicates if the hand, including openings, contains 14 tiles (4th tile from kans not included).
    /// </summary>
    public bool IsFullHand => _declaredCombinations.Count * 3 + _concealedTiles.Count == 14;

    #endregion Inferred properties

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="tiles">Initial list of <see cref="TilePivot"/> (13).</param>
    internal HandPivot(IReadOnlyList<TilePivot> tiles)
    {
        LatestPick = tiles.Last();
        _concealedTiles = tiles.OrderBy(t => t).ToList();
        _declaredCombinations = new List<TileComboPivot>(4);
    }

    #region Static methods

    private static readonly int[] ImpliesSingles = new[] { 1, 4, 7, 10 };
    private static readonly int[] ImpliesPairs = new[] { 2, 5, 8, 11 };
    private static readonly int[] TwoOrThreeTiles = new[] { 2, 3 };

    /// <summary>
    /// Checks if the specified list of tiles forms a complete hand.
    /// </summary>
    /// <param name="tiles">List of tiles (other than <paramref name="declaredCombinations"/>).</param>
    /// <param name="declaredCombinations">List of declared combinations.</param>
    /// <returns><c>True</c> if complete; <c>False</c> otherwise.</returns>
    internal static bool IsCompleteFull(IReadOnlyList<TilePivot> tiles, IReadOnlyList<TileComboPivot> declaredCombinations)
    {
        return IsCompleteBasic(tiles, declaredCombinations.Count)
            || IsSevenPairs(tiles)
            || IsThirteenOrphans(tiles);
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
            || (c.Family == Families.Wind && (c.Tiles.First().Wind == dominantWind || c.Tiles.First().Wind == playerWind))
        ));
    }

    /// <summary>
    /// Checks if the specified tiles form a valid "Kokushi musou" (thirteen orphans).
    /// </summary>
    /// <param name="tiles">List of tiles.</param>
    /// <returns><c>True</c> if "Kokushi musou"; <c>False</c> otherwise.</returns>
    internal static bool IsThirteenOrphans(IReadOnlyList<TilePivot> tiles)
    {
        return tiles.Count == 14 && tiles.All(t => t.IsHonorOrTerminal) && tiles.Distinct().Count() == 13;
    }

    /// <summary>
    /// Checks if the specified tiles form a valid "Chiitoitsu" (seven pairs).
    /// </summary>
    /// <param name="tiles">List of tiles.</param>
    /// <returns><c>True</c> if "Chiitoitsu"; <c>False</c> otherwise.</returns>
    internal static bool IsSevenPairs(IReadOnlyList<TilePivot> tiles)
    {
        return tiles.Count == 14 && tiles.GroupBy(t => t).All(t => t.Count() == 2);
    }

    /// <summary>
    /// Checks if the specified tiles form a complete hand (four combinations of three tiles and a pair).
    /// "Kokushi musou" and "Chiitoitsu" must be checked separately.
    /// </summary>
    /// <param name="concealedTiles">List of concealed tiles.</param>
    /// <param name="declaredCombinationsCount">Count of declared combinations.</param>
    /// <returns>True if the hand is complete.</returns>
    internal static bool IsCompleteBasic(IReadOnlyList<TilePivot> concealedTiles, int declaredCombinationsCount)
    {
        // Every combinations are declared.
        if (declaredCombinationsCount == 4)
        {
            // The last two should form a pair.
            return concealedTiles[0] == concealedTiles[1];
        }

        var combinationsSequences = GetCombinationsSequences(concealedTiles);

        return combinationsSequences.Any(cs => cs.Count == 5 - declaredCombinationsCount && cs.Count(c => c.IsPair) == 1);
    }

    /// <summary>
    /// Checks if the specified tiles form a complete hand (four combinations of three tiles and a pair).
    /// "Kokushi musou" and "Chiitoitsu" must be checked separately.
    /// </summary>
    /// <param name="concealedTiles">List of concealed tiles.</param>
    /// <param name="declaredCombinations">List of declared combinations.</param>
    /// <returns>A list of every valid sequences of combinations.</returns>
    internal static IReadOnlyList<List<TileComboPivot>> IsCompleteBasic(IReadOnlyList<TilePivot> concealedTiles, IReadOnlyList<TileComboPivot> declaredCombinations)
    {
        // Every combinations are declared.
        if (declaredCombinations.Count == 4)
        {
            return concealedTiles[0] == concealedTiles[1]
                ? new List<List<TileComboPivot>>
                {
                    new List<TileComboPivot>(declaredCombinations)
                    {
                        new TileComboPivot(concealedTiles)
                    }
                }
                : new List<List<TileComboPivot>>();
        }

        var combinationsSequences = GetCombinationsSequences(concealedTiles);

        // Adds the declared combinations to each sequence of combinations.
        foreach (var combinationsSequence in combinationsSequences)
        {
            combinationsSequence.AddRange(declaredCombinations);
        }

        // Filters invalid sequences :
        // - Doesn't contain exactly 5 combinations.
        // - Doesn't contain a pair.
        // - Contains more than one pair.
        combinationsSequences.RemoveAll(cs => cs.Count != 5 || cs.Count(c => c.IsPair) != 1);

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
    /// <returns><c>True</c> if tenpai; <c>False</c> otherwise.</returns>
    internal static bool IsTenpai(IReadOnlyList<TilePivot> concealedTiles, IReadOnlyList<TileComboPivot> combinations, IReadOnlyList<TilePivot> notInHandTiles)
    {
        return notInHandTiles.Any(sub => IsCompleteFull(new List<TilePivot>(concealedTiles) { sub }, combinations));
    }

    // Gets every possible combinations from the given list of tiles
    private static List<List<TileComboPivot>> GetCombinationsSequences(IReadOnlyList<TilePivot> concealedTiles)
    {
        // bad approximation of size
        var combinationsSequences = new List<List<TileComboPivot>>(20);

        // Creates a group for each family
        var familyGroups = concealedTiles.GroupBy(t => t.Family);

        // The first case is not possible because its implies a single tile or several pairs.
        // The second case is not possible more than once because its implies a pair.
        var isSingle = false;
        var pairCount = 0;
        foreach (var fg in familyGroups)
        {
            var count = fg.Count();
            if (ImpliesSingles.Contains(count))
            {
                isSingle = true;
                break;
            }
            if (ImpliesPairs.Contains(count))
            {
                pairCount++;
                if (pairCount > 1)
                    break;
            }
        }

        if (!isSingle && pairCount <= 1)
        {
            foreach (var familyGroup in familyGroups)
            {
                switch (familyGroup.Key)
                {
                    case Families.Dragon:
                        CheckHonorsForCombinations(familyGroup, k => k.Dragon!.Value, combinationsSequences);
                        break;
                    case Families.Wind:
                        CheckHonorsForCombinations(familyGroup, t => t.Wind!.Value, combinationsSequences);
                        break;
                    default:
                        var temporaryCombinationsSequences = GetCombinationSequencesRecursive(familyGroup);
                        // Cartesian product of existant sequences and temporary list.
                        combinationsSequences = combinationsSequences.Count > 0 ?
                            combinationsSequences.CartesianProduct(temporaryCombinationsSequences) : temporaryCombinationsSequences;
                        break;
                }
            }
        }

        return combinationsSequences;
    }

    // Builds combinations (pairs and brelans) from dragon family or wind family.
    private static void CheckHonorsForCombinations<T>(IEnumerable<TilePivot> familyGroup,
        Func<TilePivot, T> groupKeyFunc, List<List<TileComboPivot>> combinationsSequences)
    {
        var combinations =
            familyGroup
                .GroupBy(groupKeyFunc)
                .Where(sg => TwoOrThreeTiles.Contains(sg.Count()))
                .Select(sg => new TileComboPivot(sg))
                .ToList();

        if (combinations.Count > 0)
        {
            if (combinationsSequences.Count == 0)
            {
                // Creates a new sequence of combinations, if empty at this point.
                combinationsSequences.Add(combinations);
            }
            else
            {
                // Adds the list of combinations to each existant sequence.
                combinationsSequences.ForEach(cs => cs.AddRange(combinations));
            }
        }
    }

    // Assumes that all tiles are from the same family, and this family is caracter / circle / bamboo.
    // Also assumes that referenced tile is included in the list.
    private static IReadOnlyList<TileComboPivot> GetCombinationsForTile(TilePivot tile, IEnumerable<TilePivot> tiles)
    {
        var combinations = new List<TileComboPivot>(5);

        var sameNumber = tiles.Count(t => t.Number == tile.Number);

        if (sameNumber > 1)
        {
            // Can make a pair.
            combinations.Add(new TileComboPivot(tile, tile));
            if (sameNumber > 2)
            {
                // Can make a brelan.
                combinations.Add(new TileComboPivot(tile, tile, tile));
            }
        }

        TilePivot? secondLow = null;
        TilePivot? firstLow = null;
        TilePivot? firstHigh = null;
        TilePivot? secondHigh = null;
        var count = 0;
        foreach (var t in tiles)
        {
            if (t.Number == tile.Number - 2)
            {
                secondLow = t;
                count++;
            }
            else if (t.Number == tile.Number - 1)
            {
                firstLow = t;
                count++;
            }
            else if(t.Number == tile.Number + 1)
            {
                firstHigh = t;
                count++;
            }
            else if (t.Number == tile.Number + 2)
            {
                secondHigh = t;
                count++;
            }
            if (count == 4)
                break;
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

    // Assumes that all tiles are from the same family, and this family is caracter / circle / bamboo.
    private static List<List<TileComboPivot>> GetCombinationSequencesRecursive(IEnumerable<TilePivot> tiles)
    {
        var combinationsSequences = new List<List<TileComboPivot>>(10);

        var distinctNumbers = tiles.Select(tg => tg.Number).Distinct().OrderBy(v => v).ToList();

        foreach (var number in distinctNumbers)
        {
            var combinations = GetCombinationsForTile(tiles.First(fg => fg.Number == number), tiles);
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

    #endregion Static methods

    /// <summary>
    /// Checks if <see cref="Yakus"/> and <see cref="YakusCombinations"/> have to be cancelled because of the furiten rule.
    /// </summary>
    /// <param name="discard">The discard of the current player.</param>
    /// <param name="opponentDiscards">
    /// Aggregation of discards from opponents since the riichi call; includes tiles stolen by another opponent and tiles used to call opened kan.
    /// </param>
    /// <returns><c>True</c> if furiten; <c>False</c> otherwise.</returns>
    internal bool CancelYakusIfFuriten(IReadOnlyList<TilePivot> discard, IReadOnlyList<TilePivot> opponentDiscards)
    {
        if (discard.Any(t => IsCompleteFull(new List<TilePivot>(ConcealedTiles) { t }, DeclaredCombinations.ToList())))
        {
            Yakus = null;
            YakusCombinations = null;
            return true;
        }

        if (opponentDiscards.Any(t => IsCompleteFull(new List<TilePivot>(ConcealedTiles) { t }, DeclaredCombinations.ToList())))
        {
            Yakus = null;
            YakusCombinations = null;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Checks if <see cref="Yakus"/> and <see cref="YakusCombinations"/> have to be cancelled because of the temporary furiten rule.
    /// </summary>
    /// <param name="currentRound">The current round</param>
    /// <param name="playerIndex">The player index of the hand.</param>
    /// <returns><c>True</c> if temporary furiten; <c>False</c> otherwise.</returns>
    internal bool CancelYakusIfTemporaryFuriten(RoundPivot currentRound, int playerIndex)
    {
        var i = 0;
        while (currentRound.PlayerIndexHistory.Count < i
            && currentRound.PlayerIndexHistory.ElementAt(i) == playerIndex.RelativePlayerIndex(-(i + 1))
            && playerIndex.RelativePlayerIndex(-(i + 1)) != playerIndex)
        {
            // The tile discarded by the latest player is the tile we ron !
            if (i > 0)
            {
                var lastFromDiscard = currentRound.GetDiscard(currentRound.PlayerIndexHistory.ElementAt(i)).LastOrDefault();
                if (lastFromDiscard != null && IsCompleteFull(new List<TilePivot>(ConcealedTiles) { lastFromDiscard }, DeclaredCombinations.ToList()))
                {
                    Yakus = null;
                    YakusCombinations = null;
                    return true;
                }
            }
            i++;
        }

        return false;
    }

    /// <summary>
    /// Computes and sets properties <see cref="Yakus"/> and <see cref="YakusCombinations"/>.
    /// </summary>
    /// <param name="context">The winning context.</param>
    internal void SetYakus(WinContextPivot context)
    {
        var concealedTiles = new List<TilePivot>(_concealedTiles);
        if (!context.DrawType.IsSelfDraw())
        {
            concealedTiles.Add(context.LatestTile!);
        }

        var tilesCount = concealedTiles.Count + _declaredCombinations.Count * 3;

        Yakus = null;
        YakusCombinations = null;

        var winningSequences = IsCompleteBasic(concealedTiles, new List<TileComboPivot>(_declaredCombinations)).ToList();
        if (IsSevenPairs(concealedTiles))
        {
            winningSequences.Add(new List<TileComboPivot>(concealedTiles.GroupBy(t => t).Select(c => new TileComboPivot(c))));
        }

        var yakusSequences = new Dictionary<IReadOnlyList<YakuPivot>, IReadOnlyList<TileComboPivot>?>();

        if (IsThirteenOrphans(concealedTiles))
        {
            var yakus = new List<YakuPivot> { YakuPivot.KokushiMusou };
            if (context.IsTenhou())
            {
                yakus.Add(YakuPivot.Tenhou);
            }
            else if (context.IsChiihou())
            {
                yakus.Add(YakuPivot.Chiihou);
            }
            else if (context.IsRenhou())
            {
                yakus.Add(YakuPivot.Renhou);
            }
            yakusSequences.Add(yakus, null);
        }
        else if (context.IsNagashiMangan)
        {
            yakusSequences.Add(new List<YakuPivot> { YakuPivot.NagashiMangan }, null);
        }
        else
        {
            foreach (var combinationsSequence in winningSequences)
            {
                var yakus = YakuPivot.GetYakus(combinationsSequence, context);
                yakusSequences.Add(yakus, combinationsSequence);
            }
        }

        var bestYakusSequence = YakuPivot.GetBestYakusFromList(yakusSequences.Keys, IsConcealed);

        if (bestYakusSequence.Count > 0)
        {
            Yakus = bestYakusSequence;
            YakusCombinations = yakusSequences[bestYakusSequence];
        }
    }

    /// <summary>
    /// Declares a chii. Does not discard a tile.
    /// </summary>
    /// <param name="tile">The stolen tile.</param>
    /// <param name="stolenFrom">The wind which the tile has been stolen from.</param>
    /// <param name="startNumber">The sequence first number.</param>
    internal void DeclareChii(TilePivot tile, Winds stolenFrom, int startNumber)
    {
        var tilesList = Enumerable
            .Range(startNumber, 3)
            .Where(i => i != tile.Number)
            .Select(i => _concealedTiles.FirstOrDefault(t => t.Family == tile.Family && t.Number == i))
            .Where(t => t != null)
            .Select(t => t!);

        CheckTilesForCallAndExtractCombo(tilesList, 2, tile, stolenFrom);
    }

    /// <summary>
    /// Declares a pon. Does not discard a tile.
    /// </summary>
    /// <param name="tile">The stolen tile.</param>
    /// <param name="stolenFrom">The wind which the tile has been stolen from.</param>
    internal void DeclarePon(TilePivot tile, Winds stolenFrom)
    {
        CheckTilesForCallAndExtractCombo(_concealedTiles.Where(t => t == tile), 2, tile, stolenFrom);
    }

    /// <summary>
    /// Declares a kan (opened). Does not discard a tile. Does not draw substitution tile.
    /// </summary>
    /// <param name="tile">The stolen tile.</param>
    /// <param name="stolenFrom">The wind which the tile has been stolen from.</param>
    /// <param name="fromOpenPon">The <see cref="TileComboPivot"/>, if the kan is called as an override of a previous pon call; <c>Null</c> otherwise.</param>
    internal void DeclareKan(TilePivot tile, Winds? stolenFrom, TileComboPivot fromOpenPon)
    {
        if (fromOpenPon == null)
        {
            CheckTilesForCallAndExtractCombo(_concealedTiles.Where(t => t == tile),
                stolenFrom.HasValue ? 3 : 4,
                stolenFrom.HasValue ? tile : null,
                stolenFrom
            );
        }
        else
        {
            var indexOfPon = _declaredCombinations.IndexOf(fromOpenPon);
            var concealedTiles = new List<TilePivot>
            {
                tile
            };
            concealedTiles.AddRange(fromOpenPon.Tiles.Where(t => !ReferenceEquals(t, fromOpenPon.OpenTile)));

            _declaredCombinations[indexOfPon] = new TileComboPivot(concealedTiles, fromOpenPon.OpenTile, fromOpenPon.StolenFrom);
            _concealedTiles.Remove(tile);
        }
    }

    /// <summary>
    /// Declares a kan (concealed). Does not discard a tile. Does not draw substitution tile.
    /// </summary>
    /// <param name="tile">The tile, from the current hand, to make a square from.</param>
    internal void DeclareKan(TilePivot tile)
    {
        CheckTilesForCallAndExtractCombo(_concealedTiles.Where(t => t == tile), 4, null, null);
    }

    /// <summary>
    /// Checks if a tile can be discarded, but does not discard it.
    /// </summary>
    /// <param name="tile">The tile to discard; should obviously be contained in <see cref="_concealedTiles"/>.</param>
    /// <param name="afterStealing">Optionnal; indicates if the discard is made after stealing a tile; the default value is <c>False</c>.</param>
    /// <returns><c>False</c> if the discard is forbidden by the tile stolen; <c>True</c> otherwise.</returns>
    internal bool CanDiscardTile(TilePivot tile, bool afterStealing = false)
    {
        if (afterStealing)
        {
            var lastCombination = _declaredCombinations.Last();
            var stolenTile = lastCombination.OpenTile;
            if (stolenTile == tile)
            {
                return false;
            }
            else if (lastCombination.IsSequence
                && tile.Family == lastCombination.Family
                && lastCombination.OpenTile!.Number == lastCombination.SequenceFirstNumber
                && tile.Number == lastCombination.SequenceFirstNumber + 3)
            {
                return false;
            }
            else if (lastCombination.IsSequence
                && tile.Family == lastCombination.Family
                && lastCombination.OpenTile!.Number == lastCombination.SequenceLastNumber
                && tile.Number == lastCombination.SequenceLastNumber - 3)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Tries to discard the specified tile.
    /// </summary>
    /// <param name="tile">The tile to discard; should obviously be contained in <see cref="_concealedTiles"/>.</param>
    /// <param name="afterStealing">Optionnal; indicates if the discard is made after stealing a tile; the default value is <c>False</c>.</param>
    internal void Discard(TilePivot tile, bool afterStealing = false)
    {
        _concealedTiles.Remove(tile);
    }

    /// <summary>
    /// Picks a tile from the wall (or from the treasure as compensation of a kan) and adds it to the hand.
    /// </summary>
    /// <param name="tile">The tile picked.</param>
    internal void Pick(TilePivot tile)
    {
        LatestPick = tile;
        _concealedTiles.Add(tile);
        _concealedTiles.Sort();
    }

    /// <summary>
    /// Checks if the hand is tenpai; hand must contain <c>13</c> tiles.
    /// </summary>
    /// <param name="subTiles">List of substitution tiles.</param>
    /// <param name="tileToRemoveFromConcealed">A tile to remove from the hand first.</param>
    /// <returns><c>True</c> if tenpai; <c>False</c> otherwise.</returns>
    internal bool IsTenpai(IReadOnlyList<TilePivot> subTiles, TilePivot tileToRemoveFromConcealed)
    {
        var concealedTilesCopy = ConcealedTiles;
        if (tileToRemoveFromConcealed != null)
        {
            var concealedTilesCopyList = concealedTilesCopy.ToList();
            var indexToRemove = concealedTilesCopyList.IndexOf(tileToRemoveFromConcealed);
            concealedTilesCopyList.RemoveAt(indexToRemove);
            concealedTilesCopy = concealedTilesCopyList;
        }

        return IsTenpai(concealedTilesCopy, DeclaredCombinations, subTiles);
    }

    /// <summary>
    /// Sets <see cref="LatestPick"/> after a ron.
    /// </summary>
    /// <param name="ronTile">The ron tile.</param>
    internal void SetFromRon(TilePivot ronTile)
    {
        if (Yakus == null || YakusCombinations == null || ronTile == null)
        {
            return;
        }

        LatestPick = ronTile;
    }

    // Creates a declared combination from the specified tiles
    private void CheckTilesForCallAndExtractCombo(IEnumerable<TilePivot> tiles, int expectedCount, TilePivot? tile, Winds? stolenFrom)
    {
        var tilesPick = tiles.Take(expectedCount).ToList();

        _declaredCombinations.Add(new TileComboPivot(tilesPick, tile, stolenFrom));
        tilesPick.ForEach(t => _concealedTiles.Remove(t));
    }

    /// <summary>
    /// Checks if the hand has finished on a closed wait.
    /// </summary>
    /// <returns><c>True</c> if contains a closed wait; <c>False</c> otherwise.</returns>
    internal bool HandWithClosedWait()
    {
        if (YakusCombinations == null)
        {
            return false;
        }

        // The combination with the last pick.
        var combo = YakusCombinations.FirstOrDefault(c => c.Tiles.Any(t => ReferenceEquals(t, LatestPick)));

        if (combo == null || combo.IsBrelanOrSquare)
        {
            return false;
        }

        // Other concealed (and not declared) combinations with the same tile.
        var otherCombos =
            YakusCombinations
                .Where(c => c != combo && !DeclaredCombinations.Contains(c) && c.Tiles.Contains(LatestPick))
                .ToList();

        // The real "LatestPick" is closed...
        var isClosed = combo.IsPair || LatestPick.TileIsMiddleWait(combo) || LatestPick.TileIsEdgeWait(combo);

        // .. but there might be not-closed alternatives with the same tile as "LatestPick" in other combination.
        var alternative1 = otherCombos.Any(c => c.IsBrelanOrSquare);
        var alternative2 = otherCombos.Any(c => c.IsSequence && !LatestPick.TileIsMiddleWait(c) && !LatestPick.TileIsEdgeWait(c));

        return isClosed && !(alternative1 || alternative2);
    }
}
