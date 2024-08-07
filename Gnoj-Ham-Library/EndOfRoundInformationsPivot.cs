﻿using Gnoj_Ham_Library.Enums;

namespace Gnoj_Ham_Library;

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
    internal bool Ryuukyoku { get; }
    /// <summary>
    /// <c>True</c> if the current east has not won this round.
    /// </summary>
    internal bool ToNextEast { get; }
    /// <summary>
    /// Indicates the end of the game if <c>True</c>.
    /// </summary>
    /// <remarks><c>internal</c> because it sets long after the constructor call.</remarks>
    public bool EndOfGame { get; internal set; }
    /// <summary>
    /// Indicates if the dura-dora tiles must be displayed.
    /// </summary>
    internal bool DisplayUraDora { get;  }
    /// <summary>
    /// Honba count.
    /// </summary>
    public int HonbaCount { get; }
    /// <summary>
    /// Pending riichi count.
    /// </summary>
    public int PendingRiichiCount { get; }
    /// <summary>
    /// Count of dora to display.
    /// </summary>
    public int DoraVisibleCount { get; }

    /// <summary>
    /// Informations relative to each player.
    /// </summary>
    public IReadOnlyList<PlayerInformationsPivot> PlayersInfo => _playersInfo;

    /// <summary>
    /// List of dora indicators for this round.
    /// </summary>
    public IReadOnlyList<TilePivot> DoraTiles => _doraTiles;

    /// <summary>
    /// List of uradora indicators for this round.
    /// </summary>
    public IReadOnlyList<TilePivot> UraDoraTiles => _uraDoraTiles;

    #endregion Embedded properties

    #region Inferred properties


    /// <summary>
    /// Inferred; count of uradora to display.
    /// </summary>
    public int UraDoraVisibleCount => DisplayUraDora ? DoraVisibleCount : 0;

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
    internal EndOfRoundInformationsPivot(bool ryuukyoku, bool toNextEast, bool displayUraDora,
        IReadOnlyList<PlayerInformationsPivot> playersInfo, int honbaCount, int pendingRiichiCount,
        IReadOnlyList<TilePivot> doraTiles, IReadOnlyList<TilePivot> uraDoraTiles, int doraVisibleCount)
    {
        Ryuukyoku = ryuukyoku;
        ToNextEast = toNextEast;
        DisplayUraDora = displayUraDora;
        _playersInfo = playersInfo.ToList();
        HonbaCount = honbaCount;
        PendingRiichiCount = pendingRiichiCount;
        _doraTiles = doraTiles.ToList();
        _uraDoraTiles = uraDoraTiles.ToList();
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
    public int GetPlayerPointsGain(PlayerIndices playerIndex, int defaultValue = 0)
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

        private readonly HandPivot? _hand;

        /// <summary>
        /// Indicates if CPU.
        /// </summary>
        public bool IsCpu { get; }
        /// <summary>
        /// Index in <see cref="GamePivot.Players"/>.
        /// </summary>
        public PlayerIndices Index { get; }
        /// <summary>
        /// Fan count.
        /// </summary>
        public int FanCount { get; }
        /// <summary>
        /// Fu count.
        /// </summary>
        public int FuCount { get; }
        /// <summary>
        /// The points gain from the hand itself (zero or positive).
        /// </summary>
        public int HandPointsGain { get; }
        /// <summary>
        /// Points gain for this round (might be negative).
        /// </summary>
        internal int PointsGain { get; private set; }
        /// <summary>
        /// Dora count.
        /// </summary>
        public int DoraCount { get; }
        /// <summary>
        /// Ura-dora count.
        /// </summary>
        public int UraDoraCount { get; }
        /// <summary>
        /// Red dora count.
        /// </summary>
        public int RedDoraCount { get; }

        #endregion Embedded properties

        #region Inferred properties

        /// <summary>
        /// Inferred; list of yakus in the hand.
        /// </summary>
        public IReadOnlyList<YakuPivot>? Yakus => _hand?.Yakus;

        /// <summary>
        /// Inferred; <c>True</c> if concealed hand; <c>False</c> otherwise.
        /// </summary>
        public bool Concealed => _hand?.IsConcealed == true;

        #endregion Inferred properties

        #region Constructors

        /// <summary>
        /// Constructor when winning.
        /// </summary>
        /// <param name="index">The <see cref="Index"/> value.</param>
        /// <param name="isCpu">The <see cref="IsCpu"/> value.</param>
        /// <param name="fanCount">The <see cref="FanCount"/> value.</param>
        /// <param name="fuCount">The <see cref="FuCount"/> value.</param>
        /// <param name="hand">The <see cref="_hand"/> value.</param>
        /// <param name="pointsGain">The <see cref="PointsGain"/> value.</param>
        /// <param name="doraCount">The <see cref="DoraCount"/> value.</param>
        /// <param name="uraDoraCount">The <see cref="UraDoraCount"/> value.</param>
        /// <param name="redDoraCount">The <see cref="RedDoraCount"/> value.</param>
        /// <param name="handPointsGain">The <see cref="HandPointsGain"/> value.</param>
        internal PlayerInformationsPivot(PlayerIndices index, bool isCpu, int fanCount, int fuCount, HandPivot? hand,
            int pointsGain, int doraCount, int uraDoraCount, int redDoraCount, int handPointsGain)
        {
            Index = index;
            IsCpu = isCpu;
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
        /// <param name="isCpu">The <see cref="IsCpu"/> value.</param>
        /// <param name="pointsGain">The <see cref="PointsGain"/> value.</param>
        internal PlayerInformationsPivot(PlayerIndices index, bool isCpu, int pointsGain)
            : this(index, isCpu, 0, 0, null, pointsGain, 0, 0, 0, 0) { }

        #endregion Constructors

        #region Public methods

        /// <summary>
        /// Gets tiles from the hand (if <see cref="_hand"/> not <c>Null</c>) ordered for display on the score screen.
        /// </summary>
        /// <returns>A list of tiles with additional information:
        /// <c>isLeaned</c> indicates if the tile should be displayed leaned.
        /// <c>isWinPick</c> indicates if the tile should be displayed apart.
        /// </returns>
        public IReadOnlyList<(TilePivot tile, bool isLeaned, bool isWinPick)> GetFullHandForDisplay()
        {
            if (_hand == null)
            {
                return new List<(TilePivot tile, bool isLeaned, bool isWinPick)>();
            }

            var results = new List<(TilePivot, bool, bool)>(14);
            foreach (var t in _hand.AllTiles)
            {
                if (!ReferenceEquals(t, _hand.LatestPick) || FanCount == 0)
                {
                    var leander = _hand.DeclaredCombinations.Any(c => ReferenceEquals(c.OpenTile, t));
                    results.Add((t, leander, false));
                }
            }

            // Displays the latest pick in last, only if it's a winning hand
            if (FanCount > 0)
            {
                results.Add((_hand.LatestPick, false, true));
            }

            return results;
        }

        #endregion Public methods

        #region Internal methods

        /// <summary>
        /// Adds points to <see cref="PointsGain"/>.
        /// </summary>
        /// <param name="points">Points to add.</param>
        internal void AddPoints(int points)
        {
            PointsGain += points;
        }

        #endregion Internal methods
    }
}
