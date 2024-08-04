using Gnoj_Ham_Library;
using Gnoj_Ham_Library.Enums;

namespace Gnoj_Ham_Library_Tests
{
    public class IsComplete_Tests
    {
        [Fact]
        public void IsComplete_FullyConcealed_AverageSinglePattern()
        {
            var tilesSet = TilePivot.GetCompleteSet(false);

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

            concealedTiles = concealedTiles.OrderBy(t => new Random().NextDouble()).ToList();

            var result = HandPivot.IsCompleteBasic(concealedTiles, new List<TileComboPivot>());

            Assert.NotNull(result);
            Assert.Single(result);
            AssertFiveCombinationsIncludingOnePair(result[0]);
        }

        [Fact]
        public void IsComplete_WithDeclaredKan_ComplexMultiplePatterns()
        {
            var tilesSet = TilePivot.GetCompleteSet(false);

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

            concealedTiles = concealedTiles.OrderBy(t => new Random().NextDouble()).ToList();

            var result = HandPivot.IsCompleteBasic(concealedTiles, new List<TileComboPivot>
            {
                TileComboPivot.BuildSquare(TilePivot.GetTile(tilesSet, FamilyPivot.Dragon, dragon: DragonPivot.White))
            });

            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            result.ToList().ForEach(cg => AssertFiveCombinationsIncludingOnePair(cg));
        }

        private static void AssertFiveCombinationsIncludingOnePair(List<TileComboPivot> result)
        {
            Assert.NotNull(result);
            Assert.Equal(5, result.Count);
            Assert.Equal(1, result.Count(tc => tc.IsPair));
        }
    }
}
