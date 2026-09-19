using Gnoj_Ham_Library;

namespace Gnoj_Ham_Library_Tests;

public class ShantenCalculator_Tests
{
    private static readonly IReadOnlyList<TilePivot> TilesSet = TilePivot.GetCompleteSet(false);

    // One tile of each kind: the tiles that could complete a hand.
    private static readonly IReadOnlyList<TilePivot> EveryKind = TilesSet.DistinctBy(t => t.KindIndex).ToList();

    private static readonly IReadOnlyDictionary<int, List<TilePivot>> CopiesByKind =
        TilesSet.GroupBy(t => t.KindIndex).ToDictionary(g => g.Key, g => g.ToList());

    // "123m456p789s1122z": the numbers, then their family (m: caracters, p: circles, s: bamboos, z: honors,
    // the four winds then the three dragons).
    private static List<TilePivot> Tiles(string notation)
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

    [Theory]
    // Complete hands.
    [InlineData("123m456m789m123p11z", 0, -1)]
    [InlineData("1122m3344p5566s77z", 0, -1)]
    [InlineData("19m19p19s12345677z", 0, -1)]
    // Tenpai.
    [InlineData("123m456m789m123p1z", 0, 0)]
    [InlineData("123m456m789m12p55z", 0, 0)]
    [InlineData("1112345678999m", 0, 0)]
    [InlineData("1122m3344p5566s7z", 0, 0)]
    [InlineData("1122334455667z", 0, 0)]
    [InlineData("19m19p19s1234567z", 0, 0)]
    [InlineData("19m19p19s123456z1z", 0, 0)]
    // Iishanten and beyond.
    [InlineData("123m456m78p13s55z1z", 0, 1)]
    [InlineData("1122m3344p55s123z", 0, 1)]
    [InlineData("1m3m5m7m9m1p3p5p7p9p1s3s5s", 0, 4)]
    [InlineData("1m4m7m1p4p7p1s4s7s1z2z3z4z", 0, 6)]
    // With combinations declared: the hand has fewer tiles, and no seven pairs or thirteen orphans.
    [InlineData("123m456m12p55z", 1, 0)]
    [InlineData("11z", 4, -1)]
    [InlineData("1z", 4, 0)]
    [InlineData("1122m3344p55s", 1, 2)]
    public void Compute_GivesTheNumberOfTilesThatMissToTenpai(string hand, int declaredCombinationsCount, int expected)
    {
        var shanten = ShantenCalculatorPivot.Compute(Tiles(hand), declaredCombinationsCount);

        Assert.Equal(expected, shanten);
    }

    [Fact]
    public void Compute_WithTheCountsOfEachKind_GivesTheSameAsWithTheTiles_AndLeavesThemAsTheyWere()
    {
        var tiles = Tiles("123m456m78p13s55z1z");
        var counts = new int[TilePivot.KindsCount];
        foreach (var tile in tiles)
        {
            counts[tile.KindIndex]++;
        }
        var before = (int[])counts.Clone();

        var shanten = ShantenCalculatorPivot.Compute(counts, 0);

        Assert.Equal(ShantenCalculatorPivot.Compute(tiles, 0), shanten);
        Assert.Equal(before, counts);
    }

    // A hand as it can happen at the table: 13 or 14 concealed tiles, next to the combinations declared.
    private sealed record Scenario(List<TilePivot> Hand13, List<TilePivot> Hand14, List<TileComboPivot> Declared);

    // A complete hand (or seven pairs, or thirteen orphans), a few of its tiles then swapped for others:
    // as many hands close to tenpai as farther from it.
    private static Scenario NewScenario(Random random)
    {
        var total = new int[TilePivot.KindsCount];
        var cursors = new int[TilePivot.KindsCount];
        var declared = new List<TileComboPivot>();

        TilePivot Take(int kind) => CopiesByKind[kind][cursors[kind]++];

        var style = random.Next(20);
        if (style == 0)
        {
            // Seven pairs.
            foreach (var kind in Enumerable.Range(0, TilePivot.KindsCount).OrderBy(_ => random.Next()).Take(7))
            {
                total[kind] = TileComboPivot.PairSize;
            }
        }
        else if (style == 1)
        {
            // Thirteen orphans.
            var orphans = Enumerable.Range(0, TilePivot.KindsCount)
                .Where(k => k >= TilePivot.SuitsCount * TilePivot.MaxNumber || k % TilePivot.MaxNumber is 0 or TilePivot.MaxNumber - 1)
                .ToList();
            orphans.ForEach(k => total[k] = 1);
            total[orphans[random.Next(orphans.Count)]]++;
        }
        else
        {
            var declaredCount = random.Next(3) == 0 ? random.Next(1, 3) : 0;
            while (true)
            {
                Array.Clear(total);
                var melds = new List<int[]>();
                for (var i = 0; i < HandPivot.MeldsCount; i++)
                {
                    if (random.Next(3) == 0)
                    {
                        var kind = random.Next(TilePivot.KindsCount);
                        melds.Add(new[] { kind, kind, kind });
                    }
                    else
                    {
                        var start = (random.Next(TilePivot.SuitsCount) * TilePivot.MaxNumber) + random.Next(TilePivot.MaxNumber - 2);
                        melds.Add(new[] { start, start + 1, start + 2 });
                    }
                }

                var pair = random.Next(TilePivot.KindsCount);
                foreach (var kind in melds.SelectMany(m => m).Append(pair).Append(pair))
                {
                    total[kind]++;
                }

                if (total.All(c => c <= TilePivot.CopiesCount))
                {
                    foreach (var meld in melds.Take(declaredCount))
                    {
                        declared.Add(new TileComboPivot(meld.Select(Take).ToArray()));
                    }
                    foreach (var kind in melds.Take(declaredCount).SelectMany(m => m))
                    {
                        total[kind]--;
                    }
                    break;
                }
            }
        }

        // What is left in the hand (the declared tiles are not), and how many copies of each kind are anywhere.
        var concealed = (int[])total.Clone();
        var copiesTaken = (int[])cursors.Clone();

        for (var swaps = random.Next(4); swaps > 0; swaps--)
        {
            var out1 = PickKind(random, concealed, c => c > 0);
            var in1 = PickKind(random, concealed.Select((c, kind) => c + copiesTaken[kind]).ToArray(), c => c < TilePivot.CopiesCount);
            concealed[out1]--;
            concealed[in1]++;
        }

        var hand14 = new List<TilePivot>();
        for (var kind = 0; kind < concealed.Length; kind++)
        {
            for (var i = 0; i < concealed[kind]; i++)
            {
                hand14.Add(CopiesByKind[kind][cursors[kind]++]);
            }
        }
        hand14.Sort();

        var hand13 = new List<TilePivot>(hand14);
        hand13.RemoveAt(random.Next(hand13.Count));

        return new Scenario(hand13, hand14, declared);
    }

    private static int PickKind(Random random, int[] counts, Func<int, bool> allowed)
    {
        var kinds = Enumerable.Range(0, counts.Length).Where(k => allowed(counts[k])).ToList();
        return kinds[random.Next(kinds.Count)];
    }

    private static bool IsTenpaiByBruteForce(IReadOnlyList<TilePivot> hand13, IReadOnlyList<TileComboPivot> declared)
        => TileCombinatoricsPivot.IsTenpai(hand13, declared, EveryKind, false);

    // Some tile can be drawn and another discarded to make the hand tenpai.
    private static bool ReachesTenpaiInOneExchange(IReadOnlyList<TilePivot> hand13, IReadOnlyList<TileComboPivot> declared)
    {
        foreach (var draw in EveryKind)
        {
            var hand14 = new List<TilePivot>(hand13);
            hand14.AddSorted(draw);

            foreach (var discard in hand14.Distinct())
            {
                var hand = new List<TilePivot>(hand14);
                hand.Remove(discard);
                if (IsTenpaiByBruteForce(hand, declared))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static string Describe(Scenario scenario)
        => $"[{string.Join(" ", scenario.Hand13.Select(t => t.KindIndex))}] + [{string.Join(" ", scenario.Declared.Select(c => string.Join(",", c.Tiles.Select(t => t.KindIndex))))}]";

    [Fact]
    public void Compute_OnHandsOf13Tiles_SaysTenpaiExactlyWhenSomeTileWouldCompleteThem()
    {
        var random = new Random(1);
        var tenpaiCount = 0;

        for (var i = 0; i < 3000; i++)
        {
            var scenario = NewScenario(random);

            var shanten = ShantenCalculatorPivot.Compute(scenario.Hand13, scenario.Declared.Count);

            Assert.True(shanten >= 0, Describe(scenario));
            Assert.True(
                (shanten == 0) == IsTenpaiByBruteForce(scenario.Hand13, scenario.Declared),
                $"Shanten {shanten} for {Describe(scenario)}");
            tenpaiCount += shanten == 0 ? 1 : 0;
        }

        // The hands generated are close enough to tenpai to have tested both answers.
        Assert.InRange(tenpaiCount, 300, 2700);
    }

    [Fact]
    public void Compute_OnHandsOf14Tiles_SaysCompleteExactlyWhenTheHandIsComplete()
    {
        var random = new Random(2);
        var completeCount = 0;

        for (var i = 0; i < 3000; i++)
        {
            var scenario = NewScenario(random);
            var rest = scenario.Hand14.Take(scenario.Hand14.Count - 1).ToList();

            var shanten = ShantenCalculatorPivot.Compute(scenario.Hand14, scenario.Declared.Count);

            var complete = TileCombinatoricsPivot.IsCompleteFull(rest, scenario.Declared, scenario.Hand14[^1]);
            Assert.True((shanten == -1) == complete, $"Shanten {shanten} for {Describe(scenario)}");
            completeCount += complete ? 1 : 0;
        }

        Assert.InRange(completeCount, 300, 2700);
    }

    [Fact]
    public void Compute_OnHandsOf13Tiles_SaysIishantenExactlyWhenAnExchangeOfTilesMakesThemTenpai()
    {
        var random = new Random(3);
        var iishantenCount = 0;
        var checkedCount = 0;

        while (checkedCount < 150)
        {
            var scenario = NewScenario(random);
            var shanten = ShantenCalculatorPivot.Compute(scenario.Hand13, scenario.Declared.Count);
            if (shanten == 0)
            {
                continue;
            }

            Assert.True(
                (shanten == 1) == ReachesTenpaiInOneExchange(scenario.Hand13, scenario.Declared),
                $"Shanten {shanten} for {Describe(scenario)}");
            iishantenCount += shanten == 1 ? 1 : 0;
            checkedCount++;
        }

        Assert.InRange(iishantenCount, 20, 130);
    }

    [Fact]
    public void Compute_DrawingATileLowersTheShantenByOneAtMost_AndNeverRaisesIt()
    {
        var random = new Random(4);

        for (var i = 0; i < 300; i++)
        {
            var scenario = NewScenario(random);
            var shanten = ShantenCalculatorPivot.Compute(scenario.Hand13, scenario.Declared.Count);

            foreach (var draw in EveryKind)
            {
                var hand14 = new List<TilePivot>(scenario.Hand13);
                hand14.AddSorted(draw);

                var after = ShantenCalculatorPivot.Compute(hand14, scenario.Declared.Count);

                Assert.InRange(after, shanten - 1, shanten);
            }
        }
    }
}
