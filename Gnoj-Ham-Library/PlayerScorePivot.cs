namespace Gnoj_Ham_Library;

/// <summary>
/// Represents a player score at the end of the game.
/// </summary>
public class PlayerScorePivot
{
    #region Embedded properties

    /// <summary>
    /// Ranking.
    /// </summary>
    public int Rank { get; }

    /// <summary>
    /// The player.
    /// </summary>
    public PlayerPivot Player { get; }

    /// <summary>
    /// Uma.
    /// </summary>
    public int Uma { get; }

    /// <summary>
    /// Final score; <see cref="PlayerPivot.CurrentGamePoints"/> (only thousands) plus <see cref="Uma"/>.
    /// </summary>
    public int Score { get; }

    #endregion Embedded properties

    #region Constructors

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="player">The <see cref="Player"/> value: who this score sheet gets recorded onto.</param>
    /// <param name="rank">The <see cref="Rank"/> value.</param>
    /// <param name="uma">The <see cref="Uma"/> value.</param>
    /// <param name="initialPoints">The initial points.</param>
    /// <param name="finalPoints">
    /// The points this score is actually computed from - <see cref="PlayerPivot.CurrentGamePoints"/> of
    /// whichever player actually played the game. Kept as its own parameter (rather than reading
    /// <paramref name="player"/>'s own <see cref="PlayerPivot.CurrentGamePoints"/>) so a completed
    /// game's result can be recorded onto a different, permanent <see cref="PlayerPivot"/> than the one
    /// that actually played it - see <see cref="GamePivot.ComputeCurrentRanking"/>.
    /// </param>
    internal PlayerScorePivot(PlayerPivot player, int rank, int uma, int initialPoints, int finalPoints)
    {
        Player = player;
        Rank = rank;
        Uma = uma;
        Score = ((finalPoints - initialPoints) / ScoreTools.SCORE_UNIT) + uma;

        Player.AddGameScore(this);
    }

    #endregion Constructors
}
