using System;
using System.Collections.Generic;
using System.Linq;

namespace Gnoj_Ham
{
    /// <summary>
    /// Represents informations computed at the end of a round.
    /// </summary>
    public class EndOfRoundInformationsPivot
    {
        #region Embedded properties

        private readonly List<PlayerInformationsPivot> _playersInfo;
        private readonly List<TilePivot> _doraTiles;
        private readonly List<TilePivot> _uraDoraTiles;

        /// <summary>
        /// <c>True</c> to reset <see cref="GamePivot.PendingRiichiCount"/>.
        /// </summary>
        public bool ResetRiichiPendingCount { get; private set; }
        /// <summary>
        /// <c>True</c> if the current east has not won this round.
        /// </summary>
        public bool ToNextEast { get; private set; }
        /// <summary>
        /// Indicates the end of the game if <c>True</c>.
        /// </summary>
        /// <remarks><c>internal</c> because it sets long after the constructor call.</remarks>
        public bool EndOfGame { get; internal set; }
        /// <summary>
        /// Indicates if the dura-dora tiles must be displayed.
        /// </summary>
        public bool DisplayUraDora { get; private set; }
        /// <summary>
        /// Honba count.
        /// </summary>
        public int HonbaCount { get; private set; }
        /// <summary>
        /// Pending riichi count.
        /// </summary>
        public int PendingRiichiCount { get; private set; }
        /// <summary>
        /// Count of dora to display.
        /// </summary>
        public int DoraVisibleCount { get; private set; }

        /// <summary>
        /// Informations relative to each player.
        /// </summary>
        public IReadOnlyCollection<PlayerInformationsPivot> PlayersInfo
        {
            get
            {
                return _playersInfo;
            }
        }

        /// <summary>
        /// List of dora indicators for this round.
        /// </summary>
        public IReadOnlyCollection<TilePivot> DoraTiles
        {
            get
            {
                return _doraTiles;
            }
        }

        /// <summary>
        /// List of uradora indicators for this round.
        /// </summary>
        public IReadOnlyCollection<TilePivot> UraDoraTiles
        {
            get
            {
                return _uraDoraTiles;
            }
        }

        #endregion Embedded properties

        #region Constructors

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="resetRiichiPendingCount">The <see cref="ResetRiichiPendingCount"/> value.</param>
        /// <param name="toNextEast">The <see cref="ToNextEast"/> value.</param>
        /// <param name="displayUraDora">The <see cref="DisplayUraDora"/> value.</param>
        /// <param name="playersInfo">The <see cref="_playersInfo"/> value.</param>
        /// <param name="honbaCount">The <see cref="HonbaCount"/> value.</param>
        /// <param name="pendingRiichiCount">The <see cref="PendingRiichiCount"/> value.</param>
        /// <param name="doraTiles">The <see cref="_doraTiles"/> value.</param>
        /// <param name="uraDoraTiles">The <see cref="_uraDoraTiles"/> value.</param>
        /// <param name="doraVisibleCount">The <see cref="DoraVisibleCount"/> value.</param>
        /// <exception cref="ArgumentNullException"><paramref name="playersInfo"/> is <c>Null</c>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="playersInfo"/> count is beyond <c>4</c>.</exception>
        public EndOfRoundInformationsPivot(bool resetRiichiPendingCount, bool toNextEast, bool displayUraDora,
            List<PlayerInformationsPivot> playersInfo, int honbaCount, int pendingRiichiCount,
            IEnumerable<TilePivot> doraTiles, IEnumerable<TilePivot> uraDoraTiles, int doraVisibleCount)
        {
            if (playersInfo == null)
            {
                throw new ArgumentNullException(nameof(playersInfo));
            }

            if (playersInfo.Count > 4)
            {
                throw new ArgumentOutOfRangeException(nameof(playersInfo));
            }

            ResetRiichiPendingCount = resetRiichiPendingCount;
            ToNextEast = toNextEast;
            DisplayUraDora = displayUraDora;
            _playersInfo = playersInfo;
            HonbaCount = honbaCount;
            PendingRiichiCount = pendingRiichiCount;
            _doraTiles = new List<TilePivot>(doraTiles ?? new List<TilePivot>());
            _uraDoraTiles = new List<TilePivot>(uraDoraTiles ?? new List<TilePivot>());
            DoraVisibleCount = doraVisibleCount;
        }

        #endregion Constructors

        /// <summary>
        /// Represents informations relative to one player at the end of the round.
        /// </summary>
        public class PlayerInformationsPivot
        {
            #region Embedded properties

            private readonly List<YakuPivot> _yakus;

            /// <summary>
            /// Index in <see cref="GamePivot.Players"/>.
            /// </summary>
            public int Index { get; private set; }
            /// <summary>
            /// Fan count.
            /// </summary>
            public int FanCount { get; private set; }
            /// <summary>
            /// Fu count.
            /// </summary>
            public int FuCount { get; private set; }
            /// <summary>
            /// <c>True</c> if concealed hand; <c>False</c> otherwise.
            /// </summary>
            public bool Concealed { get; private set; }
            /// <summary>
            /// Points gain for this round (might be negative).
            /// </summary>
            public int PointsGain { get; private set; }
            /// <summary>
            /// Dora count.
            /// </summary>
            public int DoraCount { get; private set; }
            /// <summary>
            /// Ura-dora count.
            /// </summary>
            public int UraDoraCount { get; private set; }
            /// <summary>
            /// Red dora count.
            /// </summary>
            public int RedDoraCount { get; private set; }

            /// <summary>
            /// List of yakus in the hand.
            /// </summary>
            public IReadOnlyCollection<YakuPivot> Yakus
            {
                get
                {
                    return _yakus;
                }
            }

            #endregion Embedded properties

            #region Constructors

            /// <summary>
            /// Constructor when winning.
            /// </summary>
            /// <param name="index">The <see cref="Index"/> value.</param>
            /// <param name="fanCount">The <see cref="FanCount"/> value.</param>
            /// <param name="fuCount">The <see cref="FuCount"/> value.</param>
            /// <param name="yakus">The <see cref="_yakus"/> value.</param>
            /// <param name="concealed">The <see cref="Concealed"/> value.</param>
            /// <param name="pointsGain">The <see cref="PointsGain"/> value.</param>
            /// <param name="doraCount">The <see cref="DoraCount"/> value.</param>
            /// <param name="uraDoraCount">The <see cref="UraDoraCount"/> value.</param>
            /// <param name="redDoraCount">The <see cref="RedDoraCount"/> value.</param>
            public PlayerInformationsPivot(int index, int fanCount, int fuCount, IEnumerable<YakuPivot> yakus, bool concealed,
                int pointsGain, int doraCount, int uraDoraCount, int redDoraCount)
            {
                Index = index;
                FanCount = fanCount;
                FuCount = fuCount;
                Concealed = concealed;
                PointsGain = pointsGain;
                DoraCount = doraCount;
                UraDoraCount = uraDoraCount;
                RedDoraCount = redDoraCount;
                _yakus = new List<YakuPivot>(yakus ?? new List<YakuPivot>());
            }

            /// <summary>
            /// Constructor when losing or neutral.
            /// </summary>
            /// <param name="index">The <see cref="Index"/> value.</param>
            /// <param name="pointsGain">The <see cref="PointsGain"/> value.</param>
            public PlayerInformationsPivot(int index, int pointsGain)
                : this(index, 0, 0, null, false, pointsGain, 0, 0, 0) { }

            #endregion Constructors
        }
    }
}
