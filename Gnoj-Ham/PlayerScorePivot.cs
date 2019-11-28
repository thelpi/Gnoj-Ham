namespace Gnoj_Ham
{
    /// <summary>
    /// Represents a player score at the end of the game.
    /// </summary>
    public class PlayerScorePivot
    {
        #region Embedded properties

        /// <summary>
        /// Ranking.
        /// </summary>
        public int Rank { get; private set; }

        /// <summary>
        /// The player.
        /// </summary>
        public PlayerPivot Player { get; private set; }

        /// <summary>
        /// Uma.
        /// </summary>
        public int Uma { get; private set; }

        /// <summary>
        /// Final score; <see cref="PlayerPivot.Points"/> (only thousands) plus <see cref="Uma"/>.
        /// </summary>
        public int Score { get; private set; }

        #endregion Embedded properties

        #region Constructors

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="player">The <see cref="Player"/> value.</param>
        /// <param name="rank">The <see cref="Rank"/> value.</param>
        /// <param name="uma">The <see cref="Uma"/> value.</param>
        /// <param name="initialPoints">The initial points.</param>
        internal PlayerScorePivot(PlayerPivot player, int rank, int uma, int initialPoints)
        {
            Player = player;
            Rank = rank;
            Uma = uma;
            Score = ((player.Points - initialPoints) / 1000) + uma;
        }

        #endregion Constructors
    }
}
