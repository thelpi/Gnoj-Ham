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
        /// <c>True</c> to "Ryuukyoku" (otherwie, resets <see cref="GamePivot.PendingRiichiCount"/>).
        /// </summary>
        public bool Ryuukyoku { get; private set; }
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

        #region Inferred properties


        /// <summary>
        /// Inferred; count of uradora to display.
        /// </summary>
        public int UraDoraVisibleCount
        {
            get
            {
                return DisplayUraDora ? DoraVisibleCount : 0;
            }
        }

        #endregion Inferred properties

        #region Constructors

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="ryuukyoku">The <see cref="Ryuukyoku"/> value.</param>
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
        internal EndOfRoundInformationsPivot(bool ryuukyoku, bool toNextEast, bool displayUraDora,
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

            Ryuukyoku = ryuukyoku;
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

        #region Public methods

        /// <summary>
        /// Get the <see cref="PlayerInformationsPivot.PointsGain"/> of the specified player index.
        /// </summary>
        /// <param name="playerIndex">Player index.</param>
        /// <param name="defaultValue">Optionnal, value to return if player not found; default value is <c>0</c>.</param>
        /// <returns>The points gain.</returns>
        public int GetPlayerPointsGain(int playerIndex, int defaultValue = 0)
        {
            return PlayersInfo.FirstOrDefault(p => p.Index == playerIndex)?.PointsGain ?? defaultValue;
        }

        #endregion Public methods

        /// <summary>
        /// Represents informations relative to one player at the end of the round.
        /// </summary>
        public class PlayerInformationsPivot
        {
            #region Embedded properties
            
            private readonly HandPivot _hand;

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
            /// The points gain from the hand itself (zero or positive).
            /// </summary>
            public int HandPointsGain { get; private set; }
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

            #endregion Embedded properties

            #region Inferred properties

            /// <summary>
            /// Inferred; list of yakus in the hand.
            /// </summary>
            public IReadOnlyCollection<YakuPivot> Yakus
            {
                get
                {
                    return _hand?.Yakus;
                }
            }

            /// <summary>
            /// Inferred; <c>True</c> if concealed hand; <c>False</c> otherwise.
            /// </summary>
            public bool Concealed
            {
                get
                {
                    return _hand?.IsConcealed == true;
                }
            }

            #endregion Inferred properties

            #region Constructors

            /// <summary>
            /// Constructor when winning.
            /// </summary>
            /// <param name="index">The <see cref="Index"/> value.</param>
            /// <param name="fanCount">The <see cref="FanCount"/> value.</param>
            /// <param name="fuCount">The <see cref="FuCount"/> value.</param>
            /// <param name="hand">The <see cref="_hand"/> value.</param>
            /// <param name="pointsGain">The <see cref="PointsGain"/> value.</param>
            /// <param name="doraCount">The <see cref="DoraCount"/> value.</param>
            /// <param name="uraDoraCount">The <see cref="UraDoraCount"/> value.</param>
            /// <param name="redDoraCount">The <see cref="RedDoraCount"/> value.</param>
            /// <param name="handPointsGain">The <see cref="HandPointsGain"/> value.</param>
            internal PlayerInformationsPivot(int index, int fanCount, int fuCount, HandPivot hand,
                int pointsGain, int doraCount, int uraDoraCount, int redDoraCount, int handPointsGain)
            {
                Index = index;
                FanCount = fanCount;
                FuCount = fuCount;
                PointsGain = pointsGain;
                DoraCount = doraCount;
                UraDoraCount = uraDoraCount;
                RedDoraCount = redDoraCount;
                HandPointsGain = handPointsGain;
                _hand = hand;
            }

            /// <summary>
            /// Constructor when losing or neutral.
            /// </summary>
            /// <param name="index">The <see cref="Index"/> value.</param>
            /// <param name="pointsGain">The <see cref="PointsGain"/> value.</param>
            internal PlayerInformationsPivot(int index, int pointsGain)
                : this(index, 0, 0, null, pointsGain, 0, 0, 0, 0) { }

            #endregion Constructors

            #region Public methods

            /// <summary>
            /// Gets tiles from the hand (if <see cref="_hand"/> not <c>Null</c>) ordered for display on the score screen.
            /// </summary>
            /// <returns>The list of tiles;
            /// the <see cref="Tuple{T1, T2, T3}.Item2"/> indicates if the tile should be displayed leaned.
            /// the <see cref="Tuple{T1, T2, T3}.Item3"/> indicates if the tile should be displayed apart.
            /// </returns>
            public List<Tuple<TilePivot, bool, bool>> GetFullHandForDisplay()
            {
                List<Tuple<TilePivot, bool, bool>> r = new List<Tuple<TilePivot, bool, bool>>();

                if (_hand != null)
                {
                    r.AddRange(_hand.AllTiles.Select(t => new Tuple<TilePivot, bool, bool>(t,
                        _hand.DeclaredCombinations.Any(c => ReferenceEquals(c.OpenTile, t)),
                        ReferenceEquals(_hand.LatestPick, t))));
                }

                return r.OrderBy(tuple => tuple.Item3).ToList();
            }

            #endregion Public methods

            #region Internal methods

            /// <summary>
            /// Adds points to <see cref="PointsGain"/>.
            /// </summary>
            /// <param name="points">Points to add.</param>
            public void AddPoints(int points)
            {
                PointsGain += points;
            }

            #endregion Internal methods
        }
    }
}
