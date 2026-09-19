using Gnoj_Ham_Library;

namespace Gnoj_Ham_Library_Tests;

/// <summary>
/// Writes a hand the way mahjong players do, for tests.
/// </summary>
internal static class HandNotation
{
    private static readonly IReadOnlyDictionary<int, List<TilePivot>> CopiesByKind =
        TilePivot.GetCompleteSet(false).GroupBy(t => t.KindIndex).ToDictionary(g => g.Key, g => g.ToList());

    /// <summary>
    /// "123m456p789s1122z": the numbers, then their family (m: caracters, p: circles, s: bamboos, z: honors,
    /// the four winds then the three dragons).
    /// </summary>
    /// <param name="notation">The hand.</param>
    /// <returns>Its tiles, sorted; each of the same kind is a different copy.</returns>
    internal static List<TilePivot> Tiles(string notation)
    {
        var used = new int[TilePivot.KindsCount];
        var tiles = new List<TilePivot>();
        var numbers = new List<int>();

        foreach (var c in notation)
        {
            if (char.IsDigit(c))
            {
                numbers.Add(c - '0');
                continue;
            }

            foreach (var number in numbers)
            {
                var kind = (c == 'z' ? TilePivot.SuitsCount : "mps".IndexOf(c)) * TilePivot.MaxNumber + number - 1;
                tiles.Add(CopiesByKind[kind][used[kind]++]);
            }
            numbers.Clear();
        }

        tiles.Sort();
        return tiles;
    }
}
