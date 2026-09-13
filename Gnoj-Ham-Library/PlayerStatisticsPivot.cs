using System.Text.Json.Serialization;

namespace Gnoj_Ham_Library;

/// <summary>
/// Player statistics, accumulated over games. Persistence is handled outside of this class.
/// </summary>
public class PlayerStatisticsPivot
{
    /// <summary>
    /// Date of the first game (with at least one round completed).
    /// </summary>
    [JsonInclude]
    public DateTime? FirstGame { get; private set; }
    /// <summary>
    /// Date of the most recent completed game (with at least one round completed).
    /// </summary>
    [JsonInclude]
    public DateTime? LastGame { get; private set; }
    /// <summary>
    /// Number of games.
    /// </summary>
    [JsonInclude]
    public int GameCount { get; private set; }
    /// <summary>
    /// Number of rounds.
    /// </summary>
    [JsonInclude]
    public int RoundCount { get; private set; }
    /// <summary>
    /// Number of games by ranking position.
    /// </summary>
    [JsonInclude]
    public int[] ByPositionCount { get; private set; } = new[] { 0, 0, 0, 0 };
    /// <summary>
    /// Number of riichi declarations.
    /// </summary>
    [JsonInclude]
    public int RiichiCount { get; private set; }
    /// <summary>
    /// Number of bankrupts.
    /// </summary>
    [JsonInclude]
    public int BankruptCount { get; private set; }
    /// <summary>
    /// Number of tsumo declarations.
    /// </summary>
    [JsonInclude]
    public int TsumoCount { get; private set; }
    /// <summary>
    /// Number of ron declarations.
    /// </summary>
    [JsonInclude]
    public int RonCount { get; private set; }
    /// <summary>
    /// Number of yakuman hands.
    /// </summary>
    [JsonInclude]
    public int YakumanCount { get; private set; }
    /// <summary>
    /// Number of opened hands.
    /// </summary>
    [JsonInclude]
    public int OpenedHandCount { get; private set; }

    /// <summary>
    /// Updates the statistics with the outcome of a round (and, if applicable, the end of the game).
    /// </summary>
    /// <param name="endOfRoundInformations">The end-of-round outcome.</param>
    /// <param name="isRon">Indicates the human player won by ron (as opposed to tsumo); irrelevant if the human player didn't win.</param>
    /// <param name="humanIsRiichi">Indicates the human player was riichi during this round.</param>
    /// <param name="humanIsConcealed">Indicates the human player's hand was still concealed at the end of the round.</param>
    /// <param name="scoreIndexPosition">The human player's rank at the end of the game; irrelevant if the game isn't over.</param>
    /// <param name="meScore">The human player's current points; irrelevant if the game isn't over.</param>
    internal void ApplyRoundResult(EndOfRoundInformationsPivot endOfRoundInformations,
        bool isRon, bool humanIsRiichi, bool humanIsConcealed, int scoreIndexPosition, int meScore)
    {
        var now = DateTime.Now;
        var pHand = endOfRoundInformations.PlayersInfo?.FirstOrDefault(_ => !_.IsCpu);

        ++RoundCount;

        if (pHand?.Yakus?.Count > 0)
        {
            YakumanCount += pHand.Yakus.Count(_ => _.IsYakuman);
            if (isRon)
                ++RonCount;
            else
                ++TsumoCount;
        }

        if (humanIsRiichi)
            ++RiichiCount;

        if (!humanIsConcealed)
            ++OpenedHandCount;

        if (endOfRoundInformations.EndOfGame)
        {
            if (meScore < 0)
                ++BankruptCount;

            ++ByPositionCount[scoreIndexPosition];
            if (!FirstGame.HasValue)
            {
                FirstGame = now;
            }
            LastGame = now;
            ++GameCount;
        }
    }
}
