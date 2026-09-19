using Gnoj_Ham_Library.Enums;

namespace Gnoj_Ham_Library;

/// <summary>
/// Represents an hand.
/// </summary>
public class HandPivot
{
    /// <summary>
    /// The melds (each counting for <see cref="TileComboPivot.MeldSize"/> tiles, even a kan) of a complete hand, besides its pair.
    /// </summary>
    internal const int MeldsCount = 4;

    /// <summary>
    /// The tiles of a complete hand.
    /// </summary>
    internal const int FullSize = (MeldsCount * TileComboPivot.MeldSize) + TileComboPivot.PairSize;

    /// <summary>
    /// The tiles of a hand as it is dealt: one less than a complete hand, which is once a tile is picked.
    /// </summary>
    internal const int DealtSize = FullSize - 1;

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
            if (!allTiles.Any(t => ReferenceEquals(t, LatestPick)))
            {
                allTiles.Add(LatestPick);
            }

            return allTiles;
        }
    }

    /// <summary>
    /// Inferred; indicates if the hand, including openings, holds a complete hand's tiles (4th tile from kans not included).
    /// </summary>
    public bool IsFullHand => (_declaredCombinations.Count * TileComboPivot.MeldSize) + _concealedTiles.Count == FullSize;

    #endregion Inferred properties

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="tiles">Initial list of <see cref="TilePivot"/> (<see cref="DealtSize"/>).</param>
    internal HandPivot(IReadOnlyList<TilePivot> tiles)
    {
        LatestPick = tiles[tiles.Count - 1];
        _concealedTiles = tiles.OrderBy(t => t).ToList();
        _declaredCombinations = new List<TileComboPivot>(MeldsCount);
    }

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
        if (discard.Any(t => TileCombinatoricsPivot.IsCompleteFull(ConcealedTiles, DeclaredCombinations.ToList(), t)))
        {
            Yakus = null;
            YakusCombinations = null;
            return true;
        }

        if (opponentDiscards.Any(t => TileCombinatoricsPivot.IsCompleteFull(ConcealedTiles, DeclaredCombinations.ToList(), t)))
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
    /// <param name="tilesSinceLastOwnDiscard">
    /// Tiles discarded by opponents since this hand owner's own last discard (the current ron tile excluded).
    /// Lasts until the hand owner's own next discard, regardless of any call made by someone else in the meantime.
    /// </param>
    /// <returns><c>True</c> if temporary furiten; <c>False</c> otherwise.</returns>
    internal bool CancelYakusIfTemporaryFuriten(IReadOnlyList<TilePivot> tilesSinceLastOwnDiscard)
    {
        if (tilesSinceLastOwnDiscard.Any(t => TileCombinatoricsPivot.IsCompleteFull(ConcealedTiles, DeclaredCombinations.ToList(), t)))
        {
            Yakus = null;
            YakusCombinations = null;
            return true;
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

        var tilesCount = concealedTiles.Count + (_declaredCombinations.Count * TileComboPivot.MeldSize);

        Yakus = null;
        YakusCombinations = null;

        var orderedConcealedTiles = concealedTiles.OrderBy(t => t).ToList();

        var winningSequences = TileCombinatoricsPivot.IsCompleteBasic(orderedConcealedTiles, new List<TileComboPivot>(_declaredCombinations)).ToList();
        if (TileCombinatoricsPivot.IsSevenPairs(orderedConcealedTiles))
        {
            winningSequences.Add(new List<TileComboPivot>(concealedTiles.GroupBy(t => t).Select(c => new TileComboPivot(c))));
        }

        var yakusSequences = new Dictionary<IReadOnlyList<YakuPivot>, IReadOnlyList<TileComboPivot>?>();

        if (TileCombinatoricsPivot.IsThirteenOrphans(orderedConcealedTiles))
        {
            // Double ("juusanmen"): before the winning tile, the hand already held all 13 different
            // types with no duplicate yet - a wait on any of the 13 at once. Any other pre-win shape
            // (a duplicate already formed, missing exactly one type) is the regular, single-tile wait.
            var preWinTiles = new List<TilePivot>(orderedConcealedTiles);
            preWinTiles.Remove(context.LatestTile!);
            var isThirteenSidedWait = preWinTiles.Distinct().Count() == 13;

            var yakus = new List<YakuPivot> { isThirteenSidedWait ? YakuPivot.KokushiMusouJuusanmen : YakuPivot.KokushiMusou };
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
            .Range(startNumber, TileComboPivot.MeldSize)
            .Where(i => i != tile.Number)
            .Select(i => _concealedTiles.FirstOrDefault(t => t.Family == tile.Family && t.Number == i))
            .Where(t => t != null)
            .Select(t => t!);

        CheckTilesForCallAndExtractCombo(tilesList, TileComboPivot.MeldSize - 1, tile, stolenFrom);
    }

    /// <summary>
    /// Declares a pon. Does not discard a tile.
    /// </summary>
    /// <param name="tile">The stolen tile.</param>
    /// <param name="stolenFrom">The wind which the tile has been stolen from.</param>
    internal void DeclarePon(TilePivot tile, Winds stolenFrom)
    {
        CheckTilesForCallAndExtractCombo(_concealedTiles.Where(t => t == tile), TileComboPivot.MeldSize - 1, tile, stolenFrom);
    }

    /// <summary>
    /// Declares a kan (opened). Does not discard a tile. Does not draw substitution tile.
    /// </summary>
    /// <param name="tile">The stolen tile.</param>
    /// <param name="stolenFrom">The wind which the tile has been stolen from.</param>
    /// <param name="fromOpenPon">The <see cref="TileComboPivot"/>, if the kan is called as an override of a previous pon call; <c>Null</c> otherwise.</param>
    internal void DeclareKan(TilePivot tile, Winds? stolenFrom, TileComboPivot? fromOpenPon)
    {
        if (fromOpenPon == null)
        {
            CheckTilesForCallAndExtractCombo(_concealedTiles.Where(t => t == tile),
                stolenFrom.HasValue ? TileComboPivot.KanSize - 1 : TileComboPivot.KanSize,
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
        CheckTilesForCallAndExtractCombo(_concealedTiles.Where(t => t == tile), TileComboPivot.KanSize, null, null);
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
            var lastCombination = _declaredCombinations[^1];
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
    /// Discards the specified tile.
    /// </summary>
    /// <param name="tile">The tile to discard; must be contained in <see cref="_concealedTiles"/>.</param>
    /// <exception cref="InvalidOperationException"><paramref name="tile"/> is not part of the concealed hand.</exception>
    internal void Discard(TilePivot tile)
    {
        // Removes this exact instance, not just any tile that's "equal" to it: TilePivot equality
        // ignores IsRedDora, so when a red and a plain copy of the same tile both sit in hand,
        // List.Remove(tile) could silently take out the wrong one (e.g. discarding the plain tile
        // client-side while the red dora actually disappears from the concealed hand).
        var index = _concealedTiles.FindIndex(t => ReferenceEquals(t, tile));
        if (index < 0)
        {
            // A silent no-op here would leave the caller (RoundPivot.Discard) advancing to the next
            // player while this tile is still sitting in the hand - a desync that would only surface
            // much later, far from its actual cause.
            throw new InvalidOperationException("The tile to discard is not part of the concealed hand.");
        }

        _concealedTiles.RemoveAt(index);
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
    internal bool IsTenpai(IReadOnlyList<TilePivot> subTiles, TilePivot? tileToRemoveFromConcealed)
    {
        var concealedTilesCopy = ConcealedTiles;
        if (tileToRemoveFromConcealed != null)
        {
            var concealedTilesCopyList = concealedTilesCopy.ToList();
            var indexToRemove = concealedTilesCopyList.IndexOf(tileToRemoveFromConcealed);
            concealedTilesCopyList.RemoveAt(indexToRemove);
            concealedTilesCopy = concealedTilesCopyList;
        }

        return TileCombinatoricsPivot.IsTenpai(concealedTilesCopy, DeclaredCombinations, subTiles, false);
    }

    /// <summary>
    /// Computes the actual wait once <paramref name="tileToRemoveFromConcealed"/> is discarded; hand
    /// must contain <c>13</c> tiles once removed.
    /// </summary>
    /// <param name="candidateTiles">List of candidate tiles (one per distinct kind is enough).</param>
    /// <param name="tileToRemoveFromConcealed">A tile to remove from the hand first.</param>
    /// <returns>The subset of <paramref name="candidateTiles"/> that would complete the hand.</returns>
    internal List<TilePivot> GetWaitTiles(IReadOnlyList<TilePivot> candidateTiles, TilePivot? tileToRemoveFromConcealed)
    {
        var concealedTilesCopy = ConcealedTiles;
        if (tileToRemoveFromConcealed != null)
        {
            var concealedTilesCopyList = concealedTilesCopy.ToList();
            var indexToRemove = concealedTilesCopyList.IndexOf(tileToRemoveFromConcealed);
            concealedTilesCopyList.RemoveAt(indexToRemove);
            concealedTilesCopy = concealedTilesCopyList;
        }

        return TileCombinatoricsPivot.GetWaitTiles(concealedTilesCopy, DeclaredCombinations, candidateTiles);
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
