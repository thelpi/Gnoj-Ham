using System;
using System.Collections.Generic;
using System.Linq;

namespace Gnoj_Ham
{
    /// <summary>
    /// Tools to compute score.
    /// </summary>
    public static class ScoreTools
    {
        #region Points chart

        // TODO : transform these constants into configuration.

        /// <summary>
        /// Riichi cost.
        /// </summary>
        public const int RIICHI_COST = 1000;

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

        #endregion Points chart

        /// <summary>
        /// Gets points repartition for a round ending in "ryuukyoku".
        /// </summary>
        /// <param name="countTenpai">Count of tenpai players.</param>
        /// <returns>Points for tenpai players; Points for non-tenpai players.</returns>
        public static Tuple<int, int> GetRyuukyokuPoints(int countTenpai)
        {
            if (countTenpai == 1)
            {
                return new Tuple<int, int>(TENPAI_BASE_POINTS * (4 - countTenpai), -TENPAI_BASE_POINTS);
            }
            else if (countTenpai == 2)
            {
                return new Tuple<int, int>(TENPAI_BASE_POINTS + TENPAI_BASE_POINTS / countTenpai, -(TENPAI_BASE_POINTS + TENPAI_BASE_POINTS / countTenpai));
            }
            else if (countTenpai == 3)
            {
                return new Tuple<int, int>(TENPAI_BASE_POINTS, countTenpai * -TENPAI_BASE_POINTS);
            }
            else
            {
                return new Tuple<int, int>(0, 0);
            }
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
        /// <exception cref="ArgumentNullException"><paramref name="yakus"/> is <c>Null</c>.</exception>
        public static int GetFanCount(IEnumerable<YakuPivot> yakus, bool concealed, int dorasCount = 0, int uraDorasCount = 0, int redDorasCount = 0)
        {
            if (yakus == null)
            {
                throw new ArgumentNullException(nameof(yakus));
            }

            int yakumansCount = yakus.Count(y => (concealed ? y.ConcealedFanCount : y.FanCount) == 13);

            if (yakumansCount > 0)
            {
                return (MULTIPLE_YAKUMANS ? yakumansCount : 1) * 13;
            }

            int initialFanCount = yakus.Sum(y => concealed ? y.ConcealedFanCount : y.FanCount) + dorasCount + uraDorasCount + redDorasCount;

            return initialFanCount >= 13 ? (ALLOW_KAZOE_YAKUMAN ? 13 : 12) : initialFanCount;
        }

        /// <summary>
        /// Computes the fu count.
        /// </summary>
        /// <param name="hand">The hand.</param>
        /// <param name="isTsumo"><c>True</c> if the winning tile is concealed; <c>False</c> otherwise.</param>
        /// <param name="dominantWind">The dominant wind;</param>
        /// <param name="playerWind">The player wind.</param>
        /// <exception cref="ArgumentNullException"><paramref name="hand"/> is <c>Null</c>.</exception>
        public static int GetFuCount(HandPivot hand, bool isTsumo, WindPivot dominantWind, WindPivot playerWind)
        {
            if (hand == null)
            {
                throw new ArgumentNullException(nameof(hand));
            }

            if (hand.Yakus.Any(y => y == YakuPivot.Chiitoitsu))
            {
                return CHIITOI_FU;
            }

            int fuCount =
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

            return (hand.IsConcealed && !isTsumo ? BASE_CONCEALED_RON_FU : BASE_FU) + fuCount;
        }

        /// <summary>
        /// Computes the number of points for a winner.
        /// </summary>
        /// <param name="fanCount">Fan count.</param>
        /// <param name="fuCount">Fu count.</param>
        /// <param name="honbaCount">Honba count.</param>
        /// <param name="winnerPlayersCount">Number of winners (divide <paramref name="honbaCount"/>).</param>
        /// <param name="isTsumo"><c>True</c> if win by tsumo; <c>False</c> otherwise.</param>
        /// <param name="playerWind">The current player wind.</param>
        /// <param name="riichiPendingCount">Riichi pending count.</param>
        /// <returns>
        /// - Number of points lost by east players (or one of the three remaining if the winner is east; or the specific loser if ron).
        /// - Number of points lost by the two other players.
        /// </returns>
        public static Tuple<int, int> GetPoints(int fanCount, int fuCount, int honbaCount, int winnerPlayersCount, bool isTsumo, WindPivot playerWind, int riichiPendingCount)
        {
            // TODO
            throw new NotImplementedException();
        }
    }
}
