using System;
using System.Collections.Generic;
using System.Linq;

namespace Gnoj_Ham
{
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
        public TilePivot LatestPick { get; private set; }

        /// <summary>
        /// Yakus, if the hand is complete; otherwise <c>Null</c>.
        /// </summary>
        public IReadOnlyList<YakuPivot> Yakus { get; private set; }

        /// <summary>
        /// Combinations computed in the hand to produce <see cref="Yakus"/>;
        /// <c>Null</c> if <see cref="Yakus"/> is <c>Null</c> or contains <see cref="YakuPivot.KokushiMusou"/> or <see cref="YakuPivot.NagashiMangan"/>.
        /// </summary>
        public IReadOnlyList<TileComboPivot> YakusCombinations { get; private set; }

        #endregion Embedded properties

        #region Inferred properties

        /// <summary>
        /// Inferred; indicates if the hand is complete (can tsumo or ron depending on context).
        /// </summary>
        public bool IsComplete => Yakus != null && Yakus.Count > 0;

        /// <summary>
        /// Inferred; indicates if the hand is concealed.
        /// </summary>
        public bool IsConcealed => !_declaredCombinations.Any(c => !c.IsConcealed);

        /// <summary>
        /// Inferred; every tiles of the hand; concealed or not; into combination or not.
        /// </summary>
        public IReadOnlyList<TilePivot> AllTiles
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

        #region Constructors

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="tiles">Initial list of <see cref="TilePivot"/> (13).</param>
        internal HandPivot(IEnumerable<TilePivot> tiles)
        {
            LatestPick = tiles.Last();
            _concealedTiles = tiles.OrderBy(t => t).ToList();
            _declaredCombinations = new List<TileComboPivot>(4);
        }

        #endregion Constructors

        #region Static methods

        /// <summary>
        /// Checks if the specified list of tiles forms a complete hand.
        /// </summary>
        /// <param name="tiles">List of tiles (other than <paramref name="declaredCombinations"/>).</param>
        /// <param name="declaredCombinations">List of declared combinations.</param>
        /// <returns><c>True</c> if complete; <c>False</c> otherwise.</returns>
        public static bool IsCompleteFull(IReadOnlyList<TilePivot> tiles, IReadOnlyList<TileComboPivot> declaredCombinations)
        {
            return IsCompleteBasic(tiles, declaredCombinations).Count > 0
                || IsSevenPairs(tiles)
                || IsThirteenOrphans(tiles);
        }

        /// <summary>
        /// Checks if the specified tiles form a valid "Kokushi musou" (thirteen orphans).
        /// </summary>
        /// <param name="tiles">List of tiles.</param>
        /// <returns><c>True</c> if "Kokushi musou"; <c>False</c> otherwise.</returns>
        public static bool IsThirteenOrphans(IReadOnlyList<TilePivot> tiles)
        {
            return tiles != null && tiles.Count == 14 && tiles.Distinct().Count() == 13 && tiles.All(t => t.IsHonorOrTerminal);
        }

        /// <summary>
        /// Checks if the specified tiles form a valid "Chiitoitsu" (seven pairs).
        /// </summary>
        /// <param name="tiles">List of tiles.</param>
        /// <returns><c>True</c> if "Chiitoitsu"; <c>False</c> otherwise.</returns>
        public static bool IsSevenPairs(IReadOnlyList<TilePivot> tiles)
        {
            return tiles != null && tiles.Count == 14 && tiles.Distinct().Count() == 7 && tiles.GroupBy(t => t).All(t => t.Count() == 2);
        }

        private static readonly int[] ImpliesSingles = new[] { 1, 4, 7, 10 };
        private static readonly int[] ImpliesPairs = new[] { 2, 5, 8, 11 };

        /// <summary>
        /// Checks if the specified tiles form a complete hand (four combinations of three tiles and a pair).
        /// "Kokushi musou" and "Chiitoitsu" must be checked separately.
        /// </summary>
        /// <param name="concealedTiles">List of concealed tiles.</param>
        /// <param name="declaredCombinations">List of declared combinations.</param>
        /// <returns>A list of every valid sequences of combinations.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="concealedTiles"/> is <c>Null</c>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="declaredCombinations"/> is <c>Null</c>.</exception>
        /// <exception cref="ArgumentException"><see cref="Messages.InvalidHandTilesCount"/></exception>
        /// <remarks>Keep the type 'List' here.</remarks>
        public static IReadOnlyList<List<TileComboPivot>> IsCompleteBasic(IReadOnlyList<TilePivot> concealedTiles, IReadOnlyList<TileComboPivot> declaredCombinations)
        {
            if (declaredCombinations == null)
            {
                throw new ArgumentNullException(nameof(declaredCombinations));
            }

            if (concealedTiles == null)
            {
                throw new ArgumentNullException(nameof(concealedTiles));
            }

            if (declaredCombinations.Count * 3 + concealedTiles.Count != 14)
            {
                throw new ArgumentException(Messages.InvalidHandTilesCount, nameof(concealedTiles));
            }

            // bad approximation of size
            var combinationsSequences = new List<List<TileComboPivot>>(20);

            // Every combinations are declared.
            if (declaredCombinations.Count == 4)
            {
                // The last two should form a pair.
                if (concealedTiles[0] == concealedTiles[1])
                {
                    combinationsSequences.Add(
                        new List<TileComboPivot>(declaredCombinations)
                        {
                            new TileComboPivot(concealedTiles)
                        });
                }
                return combinationsSequences;
            }

            // Creates a group for each family
            var familyGroups = concealedTiles.GroupBy(t => t.Family);

            // The first case is not possible because its implies a single tile or several pairs.
            // The second case is not possible more than once because its implies a pair.
            if (familyGroups.Any(fg => ImpliesSingles.Contains(fg.Count()))
                || familyGroups.Count(fg => ImpliesPairs.Contains(fg.Count())) > 1)
            {
                // Empty list.
                return combinationsSequences;
            }

            foreach (var familyGroup in familyGroups)
            {
                switch (familyGroup.Key)
                {
                    case FamilyPivot.Dragon:
                        CheckHonorsForCombinations(familyGroup, k => k.Dragon.Value, combinationsSequences);
                        break;
                    case FamilyPivot.Wind:
                        CheckHonorsForCombinations(familyGroup, t => t.Wind.Value, combinationsSequences);
                        break;
                    default:
                        var temporaryCombinationsSequences = GetCombinationSequencesRecursive(familyGroup);
                        // Cartesian product of existant sequences and temporary list.
                        combinationsSequences = combinationsSequences.Count > 0 ?
                            combinationsSequences.CartesianProduct(temporaryCombinationsSequences) : temporaryCombinationsSequences;
                        break;
                }
            }

            // Adds the declared combinations to each sequence of combinations.
            foreach (var combinationsSequence in combinationsSequences)
            {
                combinationsSequence.AddRange(declaredCombinations);
            }

            // Filters invalid sequences :
            // - Doesn't contain exactly 5 combinations.
            // - Doesn't contain a pair.
            // - Contains more than one pair.
            combinationsSequences.RemoveAll(cs => cs.Count() != 5 || cs.Count(c => c.IsPair) != 1);

            // Filters duplicates sequences
            combinationsSequences.RemoveAll(cs1 =>
                combinationsSequences.Exists(cs2 =>
                    combinationsSequences.IndexOf(cs2) < combinationsSequences.IndexOf(cs1)
                    && cs1.IsBijection(cs2)));

            return combinationsSequences;
        }

        // Builds combinations (pairs and brelans) from dragon family or wind family.
        private static void CheckHonorsForCombinations<T>(IEnumerable<TilePivot> familyGroup,
            Func<TilePivot, T> groupKeyFunc, List<List<TileComboPivot>> combinationsSequences)
        {
            var combinations =
                familyGroup
                    .GroupBy(groupKeyFunc)
                    .Where(sg => sg.Count() == 2 || sg.Count() == 3)
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

            var sameNumber = tiles.Where(t => t.Number == tile.Number).ToList();

            if (sameNumber.Count > 1)
            {
                // Can make a pair.
                combinations.Add(new TileComboPivot(new List<TilePivot>
                {
                    tile,
                    tile
                }));
                if (sameNumber.Count > 2)
                {
                    // Can make a brelan.
                    combinations.Add(new TileComboPivot(new List<TilePivot>
                    {
                        tile,
                        tile,
                        tile
                    }));
                }
            }

            var secondLow = tiles.FirstOrDefault(t => t.Number == tile.Number - 2);
            var firstLow = tiles.FirstOrDefault(t => t.Number == tile.Number - 1);
            var firstHigh = tiles.FirstOrDefault(t => t.Number == tile.Number + 1);
            var secondHigh = tiles.FirstOrDefault(t => t.Number == tile.Number + 2);

            if (secondLow != null && firstLow != null)
            {
                // Can make a sequence.
                combinations.Add(new TileComboPivot(new List<TilePivot>
                {
                    secondLow,
                    firstLow,
                    tile
                }));
            }
            if (firstLow != null && firstHigh != null)
            {
                // Can make a sequence.
                combinations.Add(new TileComboPivot(new List<TilePivot>
                {
                    firstLow,
                    tile,
                    firstHigh
                }));
            }
            if (firstHigh != null && secondHigh != null)
            {
                // Can make a sequence.
                combinations.Add(new TileComboPivot(new List<TilePivot>
                {
                    tile,
                    firstHigh,
                    secondHigh
                }));
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

        /// <summary>
        /// Computes if a hand is tenpai (any of <paramref name="notInHandTiles"/> can complete the hand, which must have 13th tiles).
        /// </summary>
        /// <param name="concealedTiles">Concealed tiles of the hand.</param>
        /// <param name="combinations">Declared combinations of the hand.</param>
        /// <param name="notInHandTiles">List of substitution tiles.</param>
        /// <returns><c>True</c> if tenpai; <c>False</c> otherwise.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="concealedTiles"/> is <c>Null</c>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="combinations"/> is <c>Null</c>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="notInHandTiles"/> is <c>Null</c>.</exception>
        public static bool IsTenpai(IEnumerable<TilePivot> concealedTiles, IReadOnlyList<TileComboPivot> combinations, IReadOnlyList<TilePivot> notInHandTiles)
        {
            _ = concealedTiles ?? throw new ArgumentNullException(nameof(concealedTiles));
            _ = combinations ?? throw new ArgumentNullException(nameof(combinations));
            _ = notInHandTiles ?? throw new ArgumentNullException(nameof(notInHandTiles));

            return notInHandTiles.Any(sub => IsCompleteFull(new List<TilePivot>(concealedTiles) { sub }, combinations));
        }

        /// <summary>
        /// Checks if the hand contains a valuable pair (dragon, dominant wind, player wind).
        /// </summary>
        /// <param name="combinations">Lsit of combinations.</param>
        /// <param name="dominantWind">The dominant wind.</param>
        /// <param name="playerWind">The player wind.</param>
        /// <returns><c>True</c> if vluable pair in the hand; <c>False</c> otherwise.</returns>
        public static bool HandWithValuablePair(IReadOnlyList<TileComboPivot> combinations, WindPivot dominantWind, WindPivot playerWind)
        {
            return combinations != null && combinations.Any(c => c.IsPair && (
                c.Family == FamilyPivot.Dragon
                || (c.Family == FamilyPivot.Wind && (c.Tiles.First().Wind == dominantWind || c.Tiles.First().Wind == playerWind))
            ));
        }

        #endregion Static methods

        #region Internal methods

        /// <summary>
        /// Checks if <see cref="Yakus"/> and <see cref="YakusCombinations"/> have to be cancelled because of the furiten rule.
        /// </summary>
        /// <param name="discard">The discard of the current player.</param>
        /// <param name="opponentDiscards">
        /// Aggregation of discards from opponents since the riichi call; includes tiles stolen by another opponent and tiles used to call opened kan.
        /// </param>
        /// <returns><c>True</c> if furiten; <c>False</c> otherwise.</returns>
        internal bool CancelYakusIfFuriten(IEnumerable<TilePivot> discard, IEnumerable<TilePivot> opponentDiscards)
        {
            if (discard?.Any(t => IsCompleteFull(new List<TilePivot>(ConcealedTiles) { t }, DeclaredCombinations.ToList())) == true)
            {
                Yakus = null;
                YakusCombinations = null;
                return true;
            }

            if (opponentDiscards?.Any(t => IsCompleteFull(new List<TilePivot>(ConcealedTiles) { t }, DeclaredCombinations.ToList())) == true)
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
            if (currentRound == null)
            {
                return false;
            }

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
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var concealedTiles = new List<TilePivot>(_concealedTiles);
            if (!context.DrawType.IsSelfDraw())
            {
                concealedTiles.Add(context.LatestTile);
            }

            if (!concealedTiles.Contains(context.LatestTile))
            {
                throw new InvalidOperationException(Messages.InvalidLatestTileContext);
            }

            var tilesCount = concealedTiles.Count + _declaredCombinations.Count * 3;
            if (tilesCount != 14 && !context.IsNagashiMangan)
            {
                throw new InvalidOperationException(Messages.InvalidHandTilesCount);
            }

            Yakus = null;
            YakusCombinations = null;

            var winningSequences = IsCompleteBasic(concealedTiles, new List<TileComboPivot>(_declaredCombinations)).ToList();
            if (IsSevenPairs(concealedTiles))
            {
                winningSequences.Add(new List<TileComboPivot>(concealedTiles.GroupBy(t => t).Select(c => new TileComboPivot(c))));
            }

            var yakusSequences = new Dictionary<IReadOnlyList<YakuPivot>, IReadOnlyList<TileComboPivot>>();

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
        /// <exception cref="ArgumentNullException"><paramref name="tile"/> is <c>Null</c>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="startNumber"/> is out of range.</exception>
        /// <exception cref="InvalidOperationException"><see cref="Messages.InvalidCall"/></exception>
        internal void DeclareChii(TilePivot tile, WindPivot stolenFrom, int startNumber)
        {
            if (tile == null)
            {
                throw new ArgumentNullException(nameof(tile));
            }

            if (startNumber < 1 || startNumber > 7)
            {
                throw new ArgumentOutOfRangeException(nameof(startNumber));
            }

            if (tile.Number < startNumber || tile.Number > startNumber + 2)
            {
                throw new InvalidOperationException(Messages.InvalidCall);
            }

            CheckTilesForCallAndExtractCombo(Enumerable
                                                .Range(startNumber, 3)
                                                .Where(i => i != tile.Number)
                                                .Select(i => _concealedTiles.FirstOrDefault(t => t.Family == tile.Family && t.Number == i))
                                                .Where(t => t != null), 2, tile, stolenFrom);
        }

        /// <summary>
        /// Declares a pon. Does not discard a tile.
        /// </summary>
        /// <param name="tile">The stolen tile.</param>
        /// <param name="stolenFrom">The wind which the tile has been stolen from.</param>
        /// <exception cref="ArgumentNullException"><paramref name="tile"/> is <c>Null</c>.</exception>
        /// <exception cref="InvalidOperationException"><see cref="Messages.InvalidCall"/></exception>
        internal void DeclarePon(TilePivot tile, WindPivot stolenFrom)
        {
            if (tile == null)
            {
                throw new ArgumentNullException(nameof(tile));
            }

            CheckTilesForCallAndExtractCombo(_concealedTiles.Where(t => t == tile), 2, tile, stolenFrom);
        }

        /// <summary>
        /// Declares a kan (opened). Does not discard a tile. Does not draw substitution tile.
        /// </summary>
        /// <param name="tile">The stolen tile.</param>
        /// <param name="stolenFrom">The wind which the tile has been stolen from.</param>
        /// <param name="fromOpenPon">The <see cref="TileComboPivot"/>, if the kan is called as an override of a previous pon call; <c>Null</c> otherwise.</param>
        /// <exception cref="ArgumentNullException"><paramref name="tile"/> is <c>Null</c>.</exception>
        /// <exception cref="InvalidOperationException"><see cref="Messages.InvalidCall"/></exception>
        internal void DeclareKan(TilePivot tile, WindPivot? stolenFrom, TileComboPivot fromOpenPon)
        {
            if (tile == null)
            {
                throw new ArgumentNullException(nameof(tile));
            }

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
                if (indexOfPon < 0 || stolenFrom.HasValue)
                {
                    throw new InvalidOperationException(Messages.InvalidCall);
                }

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
        /// <exception cref="ArgumentNullException"><paramref name="tile"/> is <c>Null</c>.</exception>
        /// <exception cref="InvalidOperationException"><see cref="Messages.InvalidCall"/></exception>
        internal void DeclareKan(TilePivot tile)
        {
            if (tile == null)
            {
                throw new ArgumentNullException(nameof(tile));
            }

            CheckTilesForCallAndExtractCombo(_concealedTiles.Where(t => t == tile), 4, null, null);
        }

        /// <summary>
        /// Checks if a tile can be discarded, but does not discard it.
        /// </summary>
        /// <param name="tile">The tile to discard; should obviously be contained in <see cref="_concealedTiles"/>.</param>
        /// <param name="afterStealing">Optionnal; indicates if the discard is made after stealing a tile; the default value is <c>False</c>.</param>
        /// <returns><c>False</c> if the discard is forbidden by the tile stolen; <c>True</c> otherwise.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="tile"/> is <c>Null</c>.</exception>
        /// <exception cref="InvalidOperationException"><see cref="Messages.ImpossibleDiscard"/></exception>
        /// <exception cref="ArgumentException"><see cref="Messages.ImpossibleStealingArgument"/></exception>
        internal bool CanDiscardTile(TilePivot tile, bool afterStealing = false)
        {
            if (tile == null)
            {
                throw new ArgumentNullException(nameof(tile));
            }

            if (!_concealedTiles.Contains(tile))
            {
                throw new InvalidOperationException(Messages.ImpossibleDiscard);
            }

            if (afterStealing)
            {
                var lastCombination = _declaredCombinations.LastOrDefault();
                if (lastCombination == null || lastCombination.IsConcealed)
                {
                    throw new ArgumentException(Messages.ImpossibleStealingArgument, nameof(afterStealing));
                }
                var stolenTile = lastCombination.OpenTile;
                if (stolenTile == tile)
                {
                    return false;
                }
                else if (lastCombination.IsSequence
                    && tile.Family == lastCombination.Family
                    && lastCombination.OpenTile.Number == lastCombination.SequenceFirstNumber
                    && tile.Number == lastCombination.SequenceFirstNumber + 3)
                {
                    return false;
                }
                else if (lastCombination.IsSequence
                    && tile.Family == lastCombination.Family
                    && lastCombination.OpenTile.Number == lastCombination.SequenceLastNumber
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
        /// <returns><c>False</c> if the discard is forbidden by the tile stolen; <c>True</c> otherwise.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="tile"/> is <c>Null</c>.</exception>
        /// <exception cref="InvalidOperationException"><see cref="Messages.ImpossibleDiscard"/></exception>
        /// <exception cref="ArgumentException"><see cref="Messages.ImpossibleStealingArgument"/></exception>
        internal bool Discard(TilePivot tile, bool afterStealing = false)
        {
            if (!CanDiscardTile(tile, afterStealing))
            {
                return false;
            }

            _concealedTiles.Remove(tile);
            return true;
        }

        /// <summary>
        /// Picks a tile from the wall (or from the treasure as compensation of a kan) and adds it to the hand.
        /// </summary>
        /// <param name="tile">The tile picked.</param>
        /// <exception cref="ArgumentNullException"><paramref name="tile"/> is <c>Null</c>.</exception>
        /// <exception cref="InvalidOperationException"><see cref="Messages.InvalidDraw"/></exception>
        internal void Pick(TilePivot tile)
        {
            if (IsFullHand)
            {
                throw new InvalidOperationException(Messages.InvalidDraw);
            }

            LatestPick = tile ?? throw new ArgumentNullException(nameof(tile));
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

        #endregion Internal methods

        #region Private methods

        // Creates a declared combination from the specified tiles
        private void CheckTilesForCallAndExtractCombo(IEnumerable<TilePivot> tiles, int expectedCount, TilePivot tile, WindPivot? stolenFrom)
        {
            if (tiles.Count() < expectedCount)
            {
                throw new InvalidOperationException(Messages.InvalidCall);
            }

            var tilesPick = tiles.Take(expectedCount).ToList();

            _declaredCombinations.Add(new TileComboPivot(tilesPick, tile, stolenFrom));
            tilesPick.ForEach(t => _concealedTiles.Remove(t));
        }

        #endregion

        #region Public methods

        /// <summary>
        /// Checks if the hand has finished on a closed wait.
        /// </summary>
        /// <returns><c>True</c> if contains a closed wait; <c>False</c> otherwise.</returns>
        public bool HandWithClosedWait()
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

        #endregion Public methods
    }
}
