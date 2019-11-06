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
            _concealedTiles = tiles.OrderBy(t => t).ToList();
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

        #region Public methods

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
        /// <exception cref="InvalidOperationException"><see cref="Messages.InvalidHandTilesCount"/></exception>
        /// <exception cref="InvalidOperationException"><see cref="Messages.InvalidLatestTileContext"/></exception>
        public List<List<YakuPivot>> GetYakus(WinContextPivot context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            int tilesCount = _concealedTiles.Count + _declaredCombinations.Count * 3 + (context.IsSimulated ? 1 : 0);
            if (tilesCount != 14)
            {
                throw new InvalidOperationException(Messages.InvalidHandTilesCount);
            }

            if (!context.IsSimulated && !_concealedTiles.Contains(context.LatestTile))
            {
                throw new InvalidOperationException(Messages.InvalidLatestTileContext);
            }

            var concealedTiles = new List<TilePivot>(_concealedTiles);
            if (context.IsSimulated)
            {
                concealedTiles.Add(context.LatestTile);
            }

            // 3 possibilities :
            // - hand is complete (4 combinations and a pair)
            // - hand is concealed and seven pairs (aka "Chiitoitsu"); in that case, we form every pairs and add an element to "regularCombinationsSequences".
            // - hand is concealed and "13 orphans" (aka "Kokushi musou").
            List<List<TileComboPivot>> regularCombinationsSequences = IsComplete(concealedTiles, new List<TileComboPivot>(_declaredCombinations));
            bool isSevenPairs = concealedTiles.Count == 14 && concealedTiles.Distinct().Count() == 7;
            if (isSevenPairs)
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
                    addYaku = regularCombinationsSequences.Any(cs => cs.Count(c => c.IsBrelanOrSquare && c.Family == FamilyPivot.Dragon) == 3);
                }
                else if (yaku == YakuPivot.Suuankou)
                {
                    addYaku = regularCombinationsSequences.Any(cs => cs.Count(c => c.IsBrelanOrSquare && c.IsConcealed && (!c.Tiles.Contains(context.LatestTile) || context.DrawType.IsSelfDraw())) == 4);
                }
                else if (yaku == YakuPivot.Shousuushii)
                {
                    addYaku = regularCombinationsSequences.Any(cs => cs.Count(c => c.IsBrelanOrSquare && c.Family == FamilyPivot.Wind) == 3 && cs.Any(c => c.IsPair && c.Family == FamilyPivot.Wind));
                }
                else if (yaku == YakuPivot.Daisuushii)
                {
                    addYaku = regularCombinationsSequences.Any(cs => cs.Count(c => c.IsBrelanOrSquare && c.Family == FamilyPivot.Wind) == 4);
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
                    addYaku = context.IsFirstTurnDraw && context.PlayerWind == WindPivot.East && context.DrawType.IsSelfDraw();
                }
                else if (yaku == YakuPivot.Chiihou)
                {
                    addYaku = context.IsFirstTurnDraw && context.PlayerWind != WindPivot.East && context.DrawType.IsSelfDraw();
                }
                else if (yaku == YakuPivot.Renhou)
                {
                    // Ron at first turn (includes chankan).
                    addYaku = context.IsFirstTurnDraw && !context.DrawType.IsSelfDraw();
                }
                else
                {
                    throw new NotImplementedException();
                }

                if (addYaku)
                {
                    yakumans.Add(yaku);
                }
            }

            // Remove yakumans with existant upgrade (it's an overkill as the only known case is "Shousuushii" vs. "Daisuushii").
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

            foreach (List<TileComboPivot> combinationsSequence in regularCombinationsSequences)
            {
                List<YakuPivot> yakusForThisSequence = new List<YakuPivot>();
                foreach (YakuPivot yaku in YakuPivot.Yakus.Where(y => !y.IsYakuman))
                {
                    bool addYaku = false;
                    int occurences = 1;
                    if (yaku == YakuPivot.Chiniisou)
                    {
                        addYaku = combinationsSequence.Select(c => c.Family).Distinct().Count() == 1
                            && !combinationsSequence.Any(c => c.IsHonor);
                    }
                    else if (yaku == YakuPivot.Haitei)
                    {
                        addYaku = context.IsRoundLastTile;
                    }
                    else if (yaku == YakuPivot.RinshanKaihou)
                    {
                        addYaku = context.DrawType == DrawTypePivot.Compensation;
                    }
                    else if (yaku == YakuPivot.Chankan)
                    {
                        addYaku = context.DrawType == DrawTypePivot.OpponentKanCall;
                    }
                    else if (yaku == YakuPivot.Tanyao)
                    {
                        addYaku = combinationsSequence.All(c => !c.HasTerminalOrHonor);
                    }
                    else if (yaku == YakuPivot.Yakuhai)
                    {
                        occurences = combinationsSequence.Count(c =>
                            c.IsBrelanOrSquare && (
                                c.Family == FamilyPivot.Dragon || (
                                    c.Family == FamilyPivot.Wind && (
                                        c.Tiles.First().Wind == context.DominantWind || c.Tiles.First().Wind == context.PlayerWind
                                    )
                                )
                            )
                        );
                        addYaku = occurences > 0;
                    }
                    else if (yaku == YakuPivot.Riichi)
                    {
                        addYaku = context.IsRiichi;
                    }
                    else if (yaku == YakuPivot.Ippatsu)
                    {
                        addYaku = context.IsIppatsu;
                    }
                    else if (yaku == YakuPivot.MenzenTsumo)
                    {
                        addYaku = context.DrawType.IsSelfDraw() && combinationsSequence.All(c => c.IsConcealed);
                    }
                    else if (yaku == YakuPivot.Honiisou)
                    {
                        addYaku = combinationsSequence.Where(c => !c.IsHonor).Select(c => c.Family).Distinct().Count() == 1;
                    }
                    else if (yaku == YakuPivot.Pinfu)
                    {
                        addYaku = combinationsSequence.Count(c => c.IsSequence && c.IsConcealed) == 4
                            && combinationsSequence.Any(c => c.IsSequence && c.Family == context.LatestTile.Family && (
                                c.SequenceFirstNumber == context.LatestTile.Number
                                || c.SequenceLastNumber == context.LatestTile.Number
                            ));
                    }
                    else if (yaku == YakuPivot.Iipeikou)
                    {
                        int sequencesCount = combinationsSequence.Count(c => c.IsSequence);
                        addYaku = combinationsSequence.All(c => c.IsConcealed) && sequencesCount >= 2
                            && combinationsSequence.Where(c => c.IsSequence).Distinct().Count() < sequencesCount;
                    }
                    else if (yaku == YakuPivot.Shousangen)
                    {
                        addYaku = combinationsSequence.Count(c => c.IsBrelanOrSquare && c.Family == FamilyPivot.Dragon) == 2
                            && combinationsSequence.Any(c => c.IsPair && c.Family == FamilyPivot.Dragon);
                    }
                    else if (yaku == YakuPivot.Honroutou)
                    {
                        addYaku = combinationsSequence.All(c => c.IsTerminal || c.IsHonor);
                    }
                    else if (yaku == YakuPivot.Chiitoitsu)
                    {
                        addYaku = combinationsSequence.All(c => c.IsPair);
                    }
                    else if (yaku == YakuPivot.Sankantsu)
                    {
                        addYaku = combinationsSequence.Count(c => c.IsSquare) == 3;
                    }
                    else if (yaku == YakuPivot.SanshokuDoukou)
                    {
                        addYaku = combinationsSequence
                                    .Where(c => c.IsBrelanOrSquare && !c.IsHonor)
                                    .GroupBy(c => c.Tiles.First().Number)
                                    .FirstOrDefault(b => b.Count() >= 3)?
                                    .Select(b => b.Family)?
                                    .Distinct()?
                                    .Count() == 3;
                    }
                    else if (yaku == YakuPivot.Sanankou)
                    {
                        addYaku = combinationsSequence.Count(c => c.IsBrelanOrSquare && c.IsConcealed && (!c.Tiles.Contains(context.LatestTile) || context.DrawType.IsSelfDraw())) == 3;
                    }
                    else if (yaku == YakuPivot.Toitoi)
                    {
                        addYaku = combinationsSequence.Count(c => c.IsBrelanOrSquare) == 4;
                    }
                    else if (yaku == YakuPivot.Ittsu)
                    {
                        List<TileComboPivot> ittsuFamilyCombos =
                            combinationsSequence
                                .Where(c => c.IsSequence)
                                .GroupBy(c => c.Family)
                                .FirstOrDefault(b => b.Count() >= 3)?
                                .ToList();

                        addYaku = ittsuFamilyCombos != null
                            && ittsuFamilyCombos.Any(c => c.SequenceFirstNumber == 1)
                            && ittsuFamilyCombos.Any(c => c.SequenceFirstNumber == 4)
                            && ittsuFamilyCombos.Any(c => c.SequenceFirstNumber == 7);
                    }
                    else if (yaku == YakuPivot.SanshokuDoujun)
                    {
                        addYaku = combinationsSequence
                                    .Where(c => c.IsSequence)
                                    .GroupBy(c => c.SequenceFirstNumber)
                                    .FirstOrDefault(b => b.Count() >= 3)?
                                    .Select(b => b.Family)?
                                    .Distinct()?
                                    .Count() == 3;
                    }
                    else if (yaku == YakuPivot.Chanta)
                    {
                        addYaku = combinationsSequence.All(c => c.HasTerminalOrHonor);
                    }
                    else if (yaku == YakuPivot.DaburuRiichi)
                    {
                        addYaku = context.IsRiichi && context.IsFirstTurnRiichi;
                    }
                    else if (yaku == YakuPivot.Ryanpeikou)
                    {
                        addYaku = combinationsSequence.All(c => c.IsConcealed)
                            && combinationsSequence.Count(c => c.IsSequence) == 4
                            && combinationsSequence.Where(c => c.IsSequence).Distinct().Count() == 2;
                    }
                    else if (yaku == YakuPivot.Junchan)
                    {
                        addYaku = combinationsSequence.All(c => c.HasTerminal);
                    }
                    else if (yaku == YakuPivot.NagashiMangan)
                    {
                        // Do nothing here, but prevents the exception below.
                    }
                    else
                    {
                        throw new NotImplementedException();
                    }

                    if (addYaku)
                    {
                        yakusForThisSequence.Add(yaku, occurences);
                    }
                }

                // Remove yakus with existant upgrade.
                yakusForThisSequence.RemoveAll(y => y.Upgrades.Any(yu => yakumans.Contains(yu)));

                yakusSequences.Add(yakusForThisSequence);
            }

            return yakusSequences;
        }

        #endregion Public methods

        #region Internal methods

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

            CheckTilesForCallAndExtractCombo(_concealedTiles.Where(t => t == tile), 3, tile, stolenFrom);
        }

        /// <summary>
        /// Declares a kan (opened). Does not discard a tile. Does not draw substitution tile.
        /// </summary>
        /// <param name="tile">The stolen tile.</param>
        /// <param name="stolenFrom">The wind which the tile has been stolen from.</param>
        /// <exception cref="ArgumentNullException"><paramref name="tile"/> is <c>Null</c>.</exception>
        /// <exception cref="InvalidOperationException"><see cref="Messages.InvalidCall"/></exception>
        internal void DeclareKan(TilePivot tile, WindPivot stolenFrom)
        {
            if (tile == null)
            {
                throw new ArgumentNullException(nameof(tile));
            }

            CheckTilesForCallAndExtractCombo(_concealedTiles.Where(t => t == tile), 3, tile, stolenFrom);
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
                TileComboPivot lastCombination = _declaredCombinations.LastOrDefault();
                if (lastCombination == null || lastCombination.IsConcealed)
                {
                    throw new ArgumentException(Messages.ImpossibleStealingArgument, nameof(afterStealing));
                }
                TilePivot stolenTile = lastCombination.OpenTile;
                if (stolenTile == tile)
                {
                    return false;
                }
                else if (lastCombination.IsSequence
                    && lastCombination.OpenTile.Number == lastCombination.SequenceFirstNumber
                    && tile.Number == lastCombination.SequenceFirstNumber + 3)
                {
                    return false;
                }
                else if (lastCombination.IsSequence
                    && lastCombination.OpenTile.Number == lastCombination.SequenceLastNumber
                    && tile.Number == lastCombination.SequenceLastNumber - 3)
                {
                    return false;
                }
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
            if (tile == null)
            {
                throw new ArgumentNullException(nameof(tile));
            }

            if (_concealedTiles.Count + _declaredCombinations.Count * 3 != 13)
            {
                throw new InvalidOperationException(Messages.InvalidDraw);
            }

            _concealedTiles.Add(tile);
            _concealedTiles.Sort();
        }

        #endregion Internal methods

        #region Private methods

        private void CheckTilesForCallAndExtractCombo(IEnumerable<TilePivot> tiles, int expectedCount, TilePivot tile, WindPivot? stolenFrom)
        {
            if (tiles.Count() < expectedCount)
            {
                throw new InvalidOperationException(Messages.InvalidCall);
            }

            List<TilePivot> tilesPick = tiles.Take(expectedCount).ToList();

            _declaredCombinations.Add(new TileComboPivot(tilesPick, tile, stolenFrom));
            tilesPick.ForEach(t => _concealedTiles.Remove(t));
        }

        #endregion
    }
}
