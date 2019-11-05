using System.Collections.Generic;
using System.Linq;
using Gnoj_Ham;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Gnoj_HamUnitTests
{
    /// <summary>
    /// Unit tests for the static method <see cref="HandPivot.IsComplete(List{TilePivot}, List{TileCombo})"/>.
    /// </summary>
    [TestClass]
    public class IsComplete_Tests
    {
        /// <summary>
        /// Tests the method with a fully concealed hand, a pair of winds, a brelan of dragons and a single family (one possible pattern, average complexity).
        /// </summary>
        [TestMethod]
        public void IsComplete_FullyConcealed_AverageSinglePattern()
        {
            List<TilePivot> tilesSet = TilePivot.GetCompleteSet(false);

            var concealedTiles = new List<TilePivot>
            {
                tilesSet.First(t => t.Family == FamilyPivot.Wind && t.Wind == WindPivot.North),
                tilesSet.First(t => t.Family == FamilyPivot.Wind && t.Wind == WindPivot.North),
                tilesSet.First(t => t.Family == FamilyPivot.Dragon && t.Dragon == DragonPivot.Red),
                tilesSet.First(t => t.Family == FamilyPivot.Dragon && t.Dragon == DragonPivot.Red),
                tilesSet.First(t => t.Family == FamilyPivot.Dragon && t.Dragon == DragonPivot.Red),
                tilesSet.First(t => t.Family == FamilyPivot.Circle && t.Number == 1),
                tilesSet.First(t => t.Family == FamilyPivot.Circle && t.Number == 1),
                tilesSet.First(t => t.Family == FamilyPivot.Circle && t.Number == 1),
                tilesSet.First(t => t.Family == FamilyPivot.Circle && t.Number == 2),
                tilesSet.First(t => t.Family == FamilyPivot.Circle && t.Number == 2),
                tilesSet.First(t => t.Family == FamilyPivot.Circle && t.Number == 3),
                tilesSet.First(t => t.Family == FamilyPivot.Circle && t.Number == 3),
                tilesSet.First(t => t.Family == FamilyPivot.Circle && t.Number == 4),
                tilesSet.First(t => t.Family == FamilyPivot.Circle && t.Number == 4)
            };

            concealedTiles = concealedTiles.OrderBy(t => GlobalTools.Randomizer.NextDouble()).ToList();

            List<List<TileCombo>> result = HandPivot.IsComplete(concealedTiles, new List<TileCombo>());

            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Count);
            AssertFiveCombinationsIncludingOnePair(result[0]);
        }

        /// <summary>
        /// Tests the method with a hand containing a declared kan and a single family (two possible patterns, high complexity).
        /// </summary>
        [TestMethod]
        public void IsComplete_WithDeclaredKan_ComplexMultiplePatterns()
        {
            List<TilePivot> tilesSet = TilePivot.GetCompleteSet(false);

            var concealedTiles = new List<TilePivot>
            {
                tilesSet.First(t => t.Family == FamilyPivot.Circle && t.Number == 1),
                tilesSet.First(t => t.Family == FamilyPivot.Circle && t.Number == 1),
                tilesSet.First(t => t.Family == FamilyPivot.Circle && t.Number == 2),
                tilesSet.First(t => t.Family == FamilyPivot.Circle && t.Number == 2),
                tilesSet.First(t => t.Family == FamilyPivot.Circle && t.Number == 3),
                tilesSet.First(t => t.Family == FamilyPivot.Circle && t.Number == 3),
                tilesSet.First(t => t.Family == FamilyPivot.Circle && t.Number == 4),
                tilesSet.First(t => t.Family == FamilyPivot.Circle && t.Number == 4),
                tilesSet.First(t => t.Family == FamilyPivot.Circle && t.Number == 4),
                tilesSet.First(t => t.Family == FamilyPivot.Circle && t.Number == 5),
                tilesSet.First(t => t.Family == FamilyPivot.Circle && t.Number == 6)
            };

            concealedTiles = concealedTiles.OrderBy(t => GlobalTools.Randomizer.NextDouble()).ToList();

            List<List<TileCombo>> result = HandPivot.IsComplete(concealedTiles, new List<TileCombo>
            {
                new TileCombo(new List<TilePivot>
                {
                    tilesSet.First(t => t.Family == FamilyPivot.Dragon && t.Dragon == DragonPivot.White),
                    tilesSet.First(t => t.Family == FamilyPivot.Dragon && t.Dragon == DragonPivot.White),
                    tilesSet.First(t => t.Family == FamilyPivot.Dragon && t.Dragon == DragonPivot.White),
                    tilesSet.First(t => t.Family == FamilyPivot.Dragon && t.Dragon == DragonPivot.White),
                })
            });

            Assert.IsNotNull(result);
            Assert.AreEqual(2, result.Count);
            result.ForEach(cg => AssertFiveCombinationsIncludingOnePair(cg));
        }

        private static void AssertFiveCombinationsIncludingOnePair(List<TileCombo> result)
        {
            Assert.IsNotNull(result);
            Assert.AreEqual(5, result.Count);
            Assert.AreEqual(1, result.Count(tc => tc.IsPair));
        }
    }
}
