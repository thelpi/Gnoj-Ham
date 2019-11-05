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
        public IReadOnlyCollection<TilePivot> ConcealedTiles
        {
            get
            {
                return _concealedTiles;
            }
        }
        /// <summary>
        /// List of declared <see cref="TileComboPivot"/>.
        /// </summary>
        public IReadOnlyCollection<TileComboPivot> DeclaredCombinations
        {
            get
            {
                return _declaredCombinations;
            }
        }

        #endregion Embedded properties

        #region Inferred properties

        /// <summary>
        /// Inferred; indicates if the hand is concealed.
        /// </summary>
        public bool IsConcealed
        {
            get
            {
                return !_declaredCombinations.Any(c => !c.IsConcealed);
            }
        }

        #endregion Inferred properties

        #region Constructors

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="tiles">Initial list of <see cref="TilePivot"/> (13).</param>
        internal HandPivot(IEnumerable<TilePivot> tiles)
        {
            _concealedTiles = tiles.ToList();
            _declaredCombinations = new List<TileComboPivot>();
        }

        #endregion Constructors

        #region Static methods

        /// <summary>
        /// Checks if the specified tiles form a valid hand (four combinations of three tiles and a pair).
        /// "Kokushi musou" and "Chiitoitsu" must be checked separately.
        /// </summary>
        /// <param name="concealedTiles">List of concealed tiles.</param>
        /// <param name="declaredCombinations">List of declared combinations.</param>
        /// <returns>A list of every valid sequences of combinations.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="concealedTiles"/> is <c>Null</c>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="declaredCombinations"/> is <c>Null</c>.</exception>
        /// <exception cref="ArgumentException"><see cref="Messages.InvalidHandTilesCount"/></exception>
        public static List<List<TileComboPivot>> IsComplete(List<TilePivot> concealedTiles, List<TileComboPivot> declaredCombinations)
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

            var combinationsSequences = new List<List<TileComboPivot>>();

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
            IEnumerable<IGrouping<FamilyPivot, TilePivot>> familyGroups = concealedTiles.GroupBy(t => t.Family);

            // The first case is not possible because its implies a single tile or several pairs.
            // The second case is not possible more than once because its implies a pair.
            if (familyGroups.Any(fg => new[] { 1, 4, 7, 10 }.Contains(fg.Count()))
                || familyGroups.Count(fg => new[] { 2, 5, 8, 11 }.Contains(fg.Count())) > 1)
            {
                // Empty list.
                return combinationsSequences;
            }
            
            foreach (IGrouping<FamilyPivot, TilePivot> familyGroup in familyGroups)
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
                        List<List<TileComboPivot>> temporaryCombinationsSequences = GetCombinationSequencesRecursive(familyGroup);
                        // Cartesian product of existant sequences and temporary list.
                        combinationsSequences = combinationsSequences.Count > 0 ?
                            combinationsSequences.CartesianProduct(temporaryCombinationsSequences) : temporaryCombinationsSequences;
                        break;
                }
            }

            // Adds the declared combinations to each sequence of combinations.
            foreach (List<TileComboPivot> combinationsSequence in combinationsSequences)
            {
                combinationsSequence.AddRange(declaredCombinations);
            }

            // Filters duplicates sequences
            combinationsSequences.RemoveAll(cs1 =>
                combinationsSequences.Exists(cs2 =>
                    combinationsSequences.IndexOf(cs2) < combinationsSequences.IndexOf(cs1)
                    && cs1.IsBijection(cs2)));

            // Filters invalid sequences :
            // - Doesn't contain exactly 5 combinations.
            // - Doesn't contain a pair.
            // - Contains more than one pair.
            combinationsSequences.RemoveAll(cs => cs.Count() != 5 || cs.Count(c => c.IsPair) != 1);

            return combinationsSequences;
        }

        // Builds combinations (pairs and brelans) from dragon family or wind family.
        private static void CheckHonorsForCombinations<T>(IEnumerable<TilePivot> familyGroup,
            Func<TilePivot, T> groupKeyFunc, List<List<TileComboPivot>> combinationsSequences)
        {
            List<TileComboPivot> combinations =
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
        private static List<TileComboPivot> GetCombinationsForTile(TilePivot tile, IEnumerable<TilePivot> tiles)
        {
            var combinations = new List<TileComboPivot>();
            
            List<TilePivot> sameNumber = tiles.Where(t => t.Number == tile.Number).ToList();

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

            TilePivot secondLow = tiles.FirstOrDefault(t =>  t.Number == tile.Number - 2);
            TilePivot firstLow = tiles.FirstOrDefault(t =>  t.Number == tile.Number - 1);
            TilePivot firstHigh = tiles.FirstOrDefault(t =>  t.Number == tile.Number + 1);
            TilePivot secondHigh = tiles.FirstOrDefault(t =>  t.Number == tile.Number + 2);

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
            var combinationsSequences = new List<List<TileComboPivot>>();

            List<byte> distinctNumbers = tiles.Select(tg => tg.Number).Distinct().OrderBy(v => v).ToList();
            foreach (byte number in distinctNumbers)
            {
                List<TileComboPivot> combinations = GetCombinationsForTile(tiles.First(fg => fg.Number == number), tiles);
                foreach (TileComboPivot combination in combinations)
                {
                    var subTiles = new List<TilePivot>(tiles);
                    foreach (TilePivot tile in combination.Tiles)
                    {
                        subTiles.Remove(tile);
                    }
                    if (subTiles.Count > 0)
                    {
                        List<List<TileComboPivot>> subCombinationsSequences = GetCombinationSequencesRecursive(subTiles);
                        foreach (List<TileComboPivot> combinationsSequence in subCombinationsSequences)
                        {
                            combinationsSequence.Add(combination);
                            combinationsSequences.Add(combinationsSequence);
                        }
                    }
                    else
                    {
                        combinationsSequences.Add(new List<TileComboPivot>() { combination });
                    }
                }
            }

            return combinationsSequences;
        }

        #endregion Static methods

        /// <summary>
        /// Computes every yakus from the current hand in the specified context.
        /// </summary>
        /// <remarks>
        /// <see cref="YakuPivot.NagashiMangan"/> is ignored; the caller will have to check it by itself.
        /// If yakumans are found, the inferior yakus are not checked, except if the only yakuman is <see cref="YakuPivot.RENHOU"/>.
        /// </remarks>
        /// <param name="context">The context.</param>
        /// <returns>List of yakus sequences; the caller will have to choose the best and apply specific rules (nagashi, renhou...).</returns>
        /// <exception cref="ArgumentNullException"><paramref name="context"/> is <c>Null</c>.</exception>
        /// <exception cref="ArgumentNullException"><see cref="ContextPivot.LatestTile"/> is <c>Null</c>.</exception>
        public List<List<YakuPivot>> GetYakus(ContextPivot context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (context.LatestTile == null)
            {
                throw new ArgumentNullException(nameof(context.LatestTile));
            }

            int tilesCount = _concealedTiles.Count + _declaredCombinations.Count * 3 + (context.IsSimulatedLatestPick ? 1 : 0);
            if (tilesCount != 14)
            {
                throw new InvalidOperationException(Messages.InvalidHandTilesCount);
            }

            if (!context.IsSimulatedLatestPick && !_concealedTiles.Contains(context.LatestTile))
            {
                throw new InvalidOperationException(Messages.InvalidLatestTileContext);
            }

            var concealedTiles = new List<TilePivot>(_concealedTiles);
            if (context.IsSimulatedLatestPick)
            {
                concealedTiles.Add(context.LatestTile);
            }

            // 3 possibilities :
            // - hand is complete (4 combinations and a pair)
            // - hand is concealed and seven pairs (aka "Chiitoitsu"); in that case, we form every pairs and add an element to "regularCombinationsSequences".
            // - hand is concealed and "13 orphans" (aka "Kokushi musou").
            List<List<TileComboPivot>> regularCombinationsSequences = IsComplete(concealedTiles, new List<TileComboPivot>(_declaredCombinations));
            if (concealedTiles.Count == 14 && concealedTiles.Distinct().Count() == 7)
            {
                // TODO: this LINQ expression might not working.
                regularCombinationsSequences.Add(new List<TileComboPivot>(concealedTiles.GroupBy(t => t).Select(c => new TileComboPivot(c))));
            }
            bool isThirteenOrphans = concealedTiles.Count == 14 && concealedTiles.Distinct().Count() == 13 && concealedTiles.All(t => t.IsHonorOrTerminal);

            if (regularCombinationsSequences.Count == 0 && !isThirteenOrphans)
            {
                // No yaku.
                return new List<List<YakuPivot>>();
            }

            List<YakuPivot> yakumans = new List<YakuPivot>();

            foreach (YakuPivot yaku in YakuPivot.Yakus.Where(y => y.IsYakuman))
            {
                bool addYaku = false;
                if (yaku == YakuPivot.KokushiMusou)
                {
                    addYaku = isThirteenOrphans;
                }
                else if (yaku == YakuPivot.Daisangen)
                {
                    addYaku = regularCombinationsSequences.Any(cs => cs.Count(c => c.IsbrelanOrSquare && c.Family == FamilyPivot.Dragon) == 3);
                }
                else if (yaku == YakuPivot.Suuankou)
                {
                    addYaku = regularCombinationsSequences.Any(cs => cs.Count(c => c.IsbrelanOrSquare && c.IsConcealed) == 4);
                }
                else if (yaku == YakuPivot.Shousuushii)
                {
                    addYaku = regularCombinationsSequences.Any(cs => cs.Count(c => c.IsbrelanOrSquare && c.Family == FamilyPivot.Wind) == 3 && cs.Any(c => c.IsPair && c.Family == FamilyPivot.Wind));
                }
                else if (yaku == YakuPivot.Daisuushii)
                {
                    addYaku = regularCombinationsSequences.Any(cs => cs.Count(c => c.IsbrelanOrSquare && c.Family == FamilyPivot.Wind) == 4);
                }
                else if (yaku == YakuPivot.Tsuuiisou)
                {
                    addYaku = regularCombinationsSequences.Any(cs => cs.All(c => c.IsHonor));
                }
                else if (yaku == YakuPivot.Ryuuiisou)
                {
                    addYaku = regularCombinationsSequences.Any(cs => cs.All(c => 
                        (c.Family == FamilyPivot.Bamboo && c.Tiles.All(t => new[] { 2, 3, 4, 6, 8 }.Contains(t.Number)))
                        || (c.Family == FamilyPivot.Dragon && c.Tiles.First().Dragon == DragonPivot.Green)
                    ));
                }
                else if (yaku == YakuPivot.Chinroutou)
                {
                    addYaku = regularCombinationsSequences.Any(cs => cs.All(c => c.IsTerminal));
                }
                else if (yaku == YakuPivot.ChuurenPoutou)
                {
                    foreach (List<TileComboPivot> combinationsSequence in regularCombinationsSequences.Where(cs => cs.All(c => c.IsConcealed) && cs.Select(c => c.Family).Distinct().Count() == 1))
                    {
                        string numberPattern = string.Join(string.Empty, combinationsSequence.SelectMany(c => c.Tiles).Select(t => t.Number).OrderBy(i => i));
                        addYaku = new[]
                        {
                            "11112345678999", "11122345678999", "11123345678999",
                            "11123445678999", "11123455678999", "11123456678999",
                            "11123456778999", "11123456788999", "11123456789999"
                        }.Contains(numberPattern);
                    }
                }
                else if (yaku == YakuPivot.Suukantsu)
                {
                    addYaku = regularCombinationsSequences.Any(cs => cs.Count(c => c.IsSquare) == 4);
                }
                else if (yaku == YakuPivot.Tenhou)
                {
                    addYaku = context.IsFirstTurnDraw && context.PlayerWind == WindPivot.East && (context.IsWallDraw || context.IsCompensationTile);
                }
                else if (yaku == YakuPivot.Chiihou)
                {
                    addYaku = context.IsFirstTurnDraw && context.PlayerWind != WindPivot.East && (context.IsWallDraw || context.IsCompensationTile);
                }
                else if (yaku == YakuPivot.Renhou)
                {
                    // Ron at first turn (includes chankan).
                    addYaku = context.IsFirstTurnDraw && !context.IsWallDraw && !context.IsCompensationTile;
                }

                if (addYaku)
                {
                    yakumans.Add(yaku);
                }
            }

            // Remove yakumans with existant upgrade (it's an overkill as the only known case is "Shousuushii" vs. "Daisuushii")
            yakumans.RemoveAll(y => y.Upgrades.Any(yu => yakumans.Contains(yu)));

            var yakusSequences = new List<List<YakuPivot>>();

            if (yakumans.Count > 0)
            {
                yakusSequences.Add(yakumans);
                // The only yakuman is the optionnal one: we continue to figure what we can do with this hand.
                // Otherwise, we only return yakumans.
                if (yakumans.Count > 1 || yakumans[0] != YakuPivot.Renhou)
                {
                    return yakusSequences;
                }
            }

            // TODO

            return yakusSequences;
        }

        /// <summary>
        /// Represents a game and round context to check yakus for the hand.
        /// </summary>
        /// <remarks>
        /// Consistency between properties is not checked.
        /// Example: <see cref="IsRiichi"/> should be <c>True</c> if <see cref="IsIppatsu"/> is <c>True</c>.
        /// </remarks>
        public class ContextPivot
        {
            /// <summary>
            /// The latest tile (from self-draw or not).
            /// </summary>
            public TilePivot LatestTile { get; set; }
            /// <summary>
            /// <c>True</c> if <see cref="LatestTile"/> is not actually in the hand (simulation).
            /// </summary>
            public bool IsSimulatedLatestPick { get; set; }
            /// <summary>
            /// <c>True</c> if <see cref="LatestTile"/> is a self-draw from the wall.
            /// </summary>
            public bool IsWallDraw { get; set; }
            /// <summary>
            /// <c>True</c> if <see cref="LatestTile"/> comes from an opponent calling kan.
            /// </summary>
            public bool IsStolenFromOpponentKan { get; set; }
            /// <summary>
            /// <c>True</c> if <see cref="LatestTile"/> is a compensation tile after a kan.
            /// </summary>
            public bool IsCompensationTile { get; set; }
            /// <summary>
            /// <c>True</c> if <see cref="LatestTile"/> was the last tile of the round (from wall or opponent discard).
            /// </summary>
            public bool IsLastTile { get; set; }
            /// <summary>
            /// <c>True</c> if the player has called riichi.
            /// </summary>
            public bool IsRiichi { get; set; }
            /// <summary>
            /// <c>True</c> if it's the first turn after calling riichi.
            /// </summary>
            public bool IsIppatsu { get; set; }
            /// <summary>
            /// The current dominant wind.
            /// </summary>
            public WindPivot DominantWind { get; set; }
            /// <summary>
            /// The current player wind.
            /// </summary>
            public WindPivot PlayerWind { get; set; }
            /// <summary>
            /// <c>True</c> if it's first turn draw (without call made).
            /// </summary>
            public bool IsFirstTurnDraw { get; set; }
        }
    }
}
