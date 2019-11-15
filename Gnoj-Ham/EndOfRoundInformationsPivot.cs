namespace Gnoj_Ham
{
    /// <summary>
    /// Represents informations computed at the end of a round.
    /// </summary>
    public class EndOfRoundInformationsPivot
    {
        #region Public properties

        /// <summary>
        /// <c>True</c> to reset <see cref="GamePivot.RiichiPendingCount"/>.
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

        #endregion Public properties

        #region Constructors

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="resetRiichiPendingCount">The <see cref="ResetRiichiPendingCount"/> value.</param>
        /// <param name="toNextEast">The <see cref="ToNextEast"/> value.</param>
        public EndOfRoundInformationsPivot(bool resetRiichiPendingCount, bool toNextEast)
        {
            ResetRiichiPendingCount = resetRiichiPendingCount;
            ToNextEast = toNextEast;
        }

        #endregion Constructors
    }
}
