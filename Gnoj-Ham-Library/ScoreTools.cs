using Gnoj_Ham_Library.Enums;

namespace Gnoj_Ham_Library;

/// <summary>
/// Tools to compute score.
/// </summary>
internal static class ScoreTools
{
    #region Points chart

    /// <summary>
    /// Riichi cost.
    /// </summary>
    internal const int RIICHI_COST = 1000;

    private const int HONBA_VALUE = 300;
    private const int TENPAI_BASE_POINTS = 1000;
    private const bool MULTIPLE_YAKUMANS = false;
    private const bool ALLOW_KAZOE_YAKUMAN = false;
    private const int HONOR_KAN_FU = 32;
    private const int REGULAR_KAN_FU = 16;
    private const int HONOR_PON_FU = 8;
    private const int REGULAR_PON_FU = 4;
    private const int OPEN_PINFU_FU = 2;
    private const int CLOSED_WAIT_FU = 2;
    private const int TSUMO_FU = 2;
    private const int VALUABLE_PAIR_FU = 2;
    private const int CHIITOI_FU = 25;
    private const int BASE_FU = 20;
    private const int BASE_CONCEALED_RON_FU = 30;

    // Minimal fan count / point lost by each opponent (x2 for east, or x3 for ron on a single player)
    private static readonly IReadOnlyDictionary<int, int> OVER_FOUR_FAN = new Dictionary<int, int>
    {
        { 5, 2000 },
        { 6, 3000 },
        { 8, 4000 },
        { 11, 6000 },
        { 13, 8000 },
    };

    // Minimal fan count / Minimal fu count / points lost by the ron opponent / points lost by east if tsumo / points lost by others if tsumo
    private static readonly IReadOnlyList<(int, int, int, int, int)> CHART_OTHER = new List<(int, int, int, int, int)>
    {
        // 1-20 and 1-25 are impossible
        (1, 030, 1000, 0500, 0300),
        (1, 040, 1300, 0700, 0400),
        (1, 050, 1600, 0800, 0400),
        (1, 060, 2000, 1000, 0500),
        (1, 070, 2300, 1200, 0600),
        (1, 080, 2600, 1300, 0700),
        (1, 090, 2900, 1500, 0800),
        (1, 100, 3200, 1600, 0800),
        (1, 110, 3600, 1800, 0900),
        (2, 020, 1300, 0700, 0400),
        // 2 fans chiitoi is impossible in tsumo.
        (2, 025, 1600, 0000, 0000),
        (2, 030, 2000, 1000, 0500),
        (2, 040, 2600, 1300, 0700),
        (2, 050, 3200, 1600, 0800),
        (2, 060, 3900, 2000, 1000),
        (2, 070, 4500, 2300, 1200),
        (2, 080, 5200, 2600, 1300),
        (2, 090, 5800, 2900, 1500),
        (2, 100, 6400, 3200, 1600),
        (2, 110, 7100, 3600, 1800),
        (3, 020, 2600, 1300, 0700),
        (3, 025, 3200, 1600, 0800),
        (3, 030, 3900, 2000, 1000),
        (3, 040, 5200, 2600, 1300),
        (3, 050, 6400, 3200, 1600),
        (3, 060, 7700, 3900, 2000),
        (4, 020, 5200, 2600, 1300),
        (4, 025, 6400, 3200, 1600),
        (4, 030, 7700, 3900, 2000)
    };

    // Minimal fan count / Minimal fu count / points lost by the ron opponent / points lost by east if tsumo / points lost by others if tsumo
    private static readonly IReadOnlyList<(int, int, int, int)> CHART_EAST = new List<(int, int, int, int)>
    {
        // 1-20 and 1-25 are impossible
        (1, 030, 1500, 0500),
        (1, 040, 2000, 0700),
        (1, 050, 2400, 0800),
        (1, 060, 2900, 1000),
        (1, 070, 3400, 1200),
        (1, 080, 3900, 1300),
        (1, 090, 4400, 1500),
        (1, 100, 4800, 1600),
        (1, 110, 5300, 1800),
        (2, 020, 2000, 0700),
        // 2 fans chiitoi is impossible in tsumo.
        (2, 025, 2400, 0000),
        (2, 030, 2900, 1000),
        (2, 040, 3900, 1300),
        (2, 050, 4800, 1600),
        (2, 060, 5800, 2000),
        (2, 070, 6800, 2300),
        (2, 080, 7700, 2600),
        (2, 090, 8700, 2900),
        (2, 100, 9600, 3200),
        (2, 110, 10600, 3600),
        (3, 020, 3900, 1300),
        (3, 025, 4800, 1600),
        (3, 030, 5800, 2000),
        (3, 040, 7700, 2600),
        (3, 050, 9600, 3200),
        (3, 060, 11600, 3900),
        (4, 020, 7700, 2600),
        (4, 025, 9600, 3200),
        (4, 030, 11600, 3900)
    };

    #endregion Points chart

    /// <summary>
    /// Gets points repartition for a round ending in "ryuukyoku".
    /// </summary>
    /// <param name="countTenpai">Count of tenpai players.</param>
    /// <returns>Points for tenpai players; Points for non-tenpai players.</returns>
    internal static (int tenpai, int nonTenpai) GetRyuukyokuPoints(int countTenpai)
    {
        return countTenpai == 1
            ? (TENPAI_BASE_POINTS * (4 - countTenpai), -TENPAI_BASE_POINTS)
            : countTenpai == 2
                ? (TENPAI_BASE_POINTS + TENPAI_BASE_POINTS / countTenpai, -(TENPAI_BASE_POINTS + TENPAI_BASE_POINTS / countTenpai))
                : countTenpai == 3 ? (TENPAI_BASE_POINTS, countTenpai * -TENPAI_BASE_POINTS) : (0, 0);
    }

    /// <summary>
    /// Computes the fan count in a winning hand.
    /// </summary>
    /// <param name="yakus">List of yakus.</param>
    /// <param name="concealed"><c>True</c> if the hand is concealed; <c>False</c> otherwise.</param>
    /// <param name="dorasCount">Optionnal; doras count.</param>
    /// <param name="uraDorasCount">Optionnal; ura-doras count.</param>
    /// <param name="redDorasCount">Optionnal; red doras count.</param>
    /// <returns>The fan count.</returns>
    internal static int GetFanCount(IReadOnlyList<YakuPivot> yakus, bool concealed, int dorasCount = 0, int uraDorasCount = 0, int redDorasCount = 0)
    {
        var yakumansCount = yakus.Count(y => (concealed ? y.ConcealedFanCount : y.FanCount) == 13);

        if (yakumansCount > 0)
        {
            return (MULTIPLE_YAKUMANS ? yakumansCount : 1) * 13;
        }

        var initialFanCount = yakus.Sum(y => concealed ? y.ConcealedFanCount : y.FanCount) + dorasCount + uraDorasCount + redDorasCount;

        return initialFanCount >= 13 ? (ALLOW_KAZOE_YAKUMAN ? 13 : 12) : initialFanCount;
    }

    /// <summary>
    /// Computes the fu count.
    /// </summary>
    /// <param name="hand">The hand.</param>
    /// <param name="isTsumo"><c>True</c> if the winning tile is concealed; <c>False</c> otherwise.</param>
    /// <param name="dominantWind">The dominant wind;</param>
    /// <param name="playerWind">The player wind.</param>
    internal static int GetFuCount(HandPivot hand, bool isTsumo, Winds dominantWind, Winds playerWind)
    {
        if (hand.Yakus.Any(y => y == YakuPivot.Chiitoitsu))
        {
            return CHIITOI_FU;
        }

        var fuCount =
            hand.YakusCombinations.Count(c => c.IsSquare && c.HasTerminalOrHonor) * HONOR_KAN_FU
            + hand.YakusCombinations.Count(c => c.IsSquare && !c.HasTerminalOrHonor) * REGULAR_KAN_FU
            + hand.YakusCombinations.Count(c => c.IsBrelan && c.HasTerminalOrHonor) * HONOR_PON_FU
            + hand.YakusCombinations.Count(c => c.IsBrelan && !c.HasTerminalOrHonor) * REGULAR_PON_FU;

        if (isTsumo && !hand.Yakus.Any(y => y == YakuPivot.Pinfu))
        {
            fuCount += TSUMO_FU;
        }

        if (HandPivot.HandWithValuablePair(hand.YakusCombinations, dominantWind, playerWind))
        {
            fuCount += VALUABLE_PAIR_FU;
        }

        if (hand.HandWithClosedWait())
        {
            fuCount += CLOSED_WAIT_FU;
        }

        if (fuCount == 0 && !hand.Yakus.Any(y => y == YakuPivot.Pinfu))
        {
            fuCount += OPEN_PINFU_FU;
        }

        var baseFu = (hand.IsConcealed && !isTsumo ? BASE_CONCEALED_RON_FU : BASE_FU) + fuCount;

        return Convert.ToInt32(Math.Ceiling(baseFu / (decimal)10) * 10);
    }

    /// <summary>
    /// Computes the number of points for a winner, without honba and riichi pending count.
    /// </summary>
    /// <param name="fanCount">Fan count.</param>
    /// <param name="fuCount">Fu count.</param>
    /// <param name="isTsumo"><c>True</c> if win by tsumo; <c>False</c> otherwise.</param>
    /// <param name="playerWind">The current player wind.</param>
    /// <returns>
    /// - Number of points lost by east players (or one of the three remaining if the winner is east; or the specific loser if ron).
    /// - Number of points lost by the two other players.
    /// </returns>
    internal static (int east, int notEast) GetPoints(int fanCount, int fuCount, bool isTsumo, Winds playerWind)
    {
        var v1 = 0;
        var v2 = 0;

        var east = playerWind == Winds.East;

        if ((fanCount == 4 && fuCount >= 40) || (fanCount == 3 && fuCount >= 70))
        {
            fanCount = 5;
        }

        if (fanCount > 4)
        {
            var basePoints = OVER_FOUR_FAN.Last(k => k.Key <= fanCount).Value * (east ? 2 : 1);
            // in case of several yakumans.
            if (fanCount > 13)
            {
                basePoints += (basePoints * ((fanCount - 13) / 13));
            }
            if (isTsumo)
            {
                v1 = basePoints * (east ? 1 : 2);
                v2 = basePoints;
            }
            else
            {
                v1 = basePoints * (east ? 3 : 4);
                v2 = 0;
            }
        }
        else if (east)
        {
            var basePts = CHART_EAST.Last(k => k.Item1 <= fanCount && k.Item2 <= fuCount);
            if (isTsumo)
            {
                v1 = basePts.Item4;
                v2 = basePts.Item4;
            }
            else
            {
                v1 = basePts.Item3;
                v2 = 0;
            }
        }
        else
        {
            var basePts = CHART_OTHER.Last(k => k.Item1 <= fanCount && k.Item2 <= fuCount);
            if (isTsumo)
            {
                v1 = basePts.Item4;
                v2 = basePts.Item5;
            }
            else
            {
                v1 = basePts.Item3;
                v2 = 0;
            }
        }

        return (v1, v2);
    }

    /// <summary>
    /// Gets the total points value of honbas .
    /// </summary>
    /// <param name="honbaCount">Honbas count.</param>
    /// <returns>Total points.</returns>
    internal static int GetHonbaPoints(int honbaCount)
        => honbaCount * HONBA_VALUE;

    /// <summary>
    /// Computes uma at the specified rank.
    /// </summary>
    /// <param name="rank">The rank.</param>
    /// <returns>Uma.</returns>
    internal static int ComputeUma(int rank)
    {
        // TODO : manage more than one rule.
        return rank == 1 ? 15 : (rank == 2 ? 5 : (rank == 3 ? -5 : -15));
    }
}
