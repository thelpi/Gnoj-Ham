using System.Collections.Generic;
using System.Linq;
using Gnoj_Ham;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Gnoj_HamUnitTests
{
    /// <summary>
    /// Unit tests for the static method <see cref="HandPivot.IsComplete(List{TilePivot}, List{TileComboPivot})"/>.
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
                TilePivot.GetTile(tilesSet, FamilyPivot.Wind, wind: WindPivot.North),
                TilePivot.GetTile(tilesSet, FamilyPivot.Wind, wind: WindPivot.North),
                TilePivot.GetTile(tilesSet, FamilyPivot.Dragon, dragon: DragonPivot.Red),
                TilePivot.GetTile(tilesSet, FamilyPivot.Dragon, dragon: DragonPivot.Red),
                TilePivot.GetTile(tilesSet, FamilyPivot.Dragon, dragon: DragonPivot.Red),
                TilePivot.GetTile(tilesSet, FamilyPivot.Circle, number: 1),
                TilePivot.GetTile(tilesSet, FamilyPivot.Circle, number: 1),
                TilePivot.GetTile(tilesSet, FamilyPivot.Circle, number: 1),
                TilePivot.GetTile(tilesSet, FamilyPivot.Circle, number: 2),
                TilePivot.GetTile(tilesSet, FamilyPivot.Circle, number: 2),
                TilePivot.GetTile(tilesSet, FamilyPivot.Circle, number: 3),
                TilePivot.GetTile(tilesSet, FamilyPivot.Circle, number: 3),
                TilePivot.GetTile(tilesSet, FamilyPivot.Circle, number: 4),
                TilePivot.GetTile(tilesSet, FamilyPivot.Circle, number: 4)
            };

            concealedTiles = concealedTiles.OrderBy(t => GlobalTools.Randomizer.NextDouble()).ToList();

            List<List<TileComboPivot>> result = HandPivot.IsComplete(concealedTiles, new List<TileComboPivot>());

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
                TilePivot.GetTile(tilesSet, FamilyPivot.Circle, number: 1),
                TilePivot.GetTile(tilesSet, FamilyPivot.Circle, number: 1),
                TilePivot.GetTile(tilesSet, FamilyPivot.Circle, number: 2),
                TilePivot.GetTile(tilesSet, FamilyPivot.Circle, number: 2),
                TilePivot.GetTile(tilesSet, FamilyPivot.Circle, number: 3),
                TilePivot.GetTile(tilesSet, FamilyPivot.Circle, number: 3),
                TilePivot.GetTile(tilesSet, FamilyPivot.Circle, number: 4),
                TilePivot.GetTile(tilesSet, FamilyPivot.Circle, number: 4),
                TilePivot.GetTile(tilesSet, FamilyPivot.Circle, number: 4),
                TilePivot.GetTile(tilesSet, FamilyPivot.Circle, number: 5),
                TilePivot.GetTile(tilesSet, FamilyPivot.Circle, number: 6)
            };

            concealedTiles = concealedTiles.OrderBy(t => GlobalTools.Randomizer.NextDouble()).ToList();

            List<List<TileComboPivot>> result = HandPivot.IsComplete(concealedTiles, new List<TileComboPivot>
            {
                TileComboPivot.BuildSquare(TilePivot.GetTile(tilesSet, FamilyPivot.Dragon, dragon: DragonPivot.White))
            });

            Assert.IsNotNull(result);
            Assert.AreEqual(2, result.Count);
            result.ForEach(cg => AssertFiveCombinationsIncludingOnePair(cg));
        }

        private static void AssertFiveCombinationsIncludingOnePair(List<TileComboPivot> result)
        {
            Assert.IsNotNull(result);
            Assert.AreEqual(5, result.Count);
            Assert.AreEqual(1, result.Count(tc => tc.IsPair));
        }
    }
}
