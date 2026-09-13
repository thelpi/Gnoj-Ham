using System.Text.Json;
using System.Text.Json.Serialization;

namespace Gnoj_Ham_Library;

/// <summary>
/// Player save file
/// </summary>
public class PlayerSavePivot
{
    private const string SAVE_FILE_NAME = "save_file.dat";

    private static string FullFileName => Path.Combine(Environment.CurrentDirectory, SAVE_FILE_NAME);

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
    /// Gets or creates the player save file.
    /// </summary>
    /// <returns>Player save file.</returns>
    public static (PlayerSavePivot save, string? error) GetOrCreateSave()
    {
        var save = new PlayerSavePivot();

        try
        {
            if (File.Exists(FullFileName))
            {
                using var stream = new FileStream(FullFileName, FileMode.Open, FileAccess.Read, FileShare.Read);
                save = JsonSerializer.Deserialize<PlayerSavePivot>(stream)
                    ?? throw new InvalidOperationException("Le fichier de sauvegarde est vide ou invalide.");
            }
        }
        catch (Exception ex)
        {
            return (save, ex.Message);
        }

        return (save, null);
    }

    private string? SavePlayerFile()
    {
        try
        {
            using var stream = new FileStream(FullFileName, FileMode.Create, FileAccess.Write, FileShare.None);
            JsonSerializer.Serialize(stream, this);
        }
        catch (Exception ex)
        {
            return ex.Message;
        }

        return null;
    }

    /// <summary>
    /// Updates the statistics with the outcome of a round (and, if applicable, the end of the game), then saves the file.
    /// </summary>
    /// <param name="endOfRoundInformations">The end-of-round outcome.</param>
    /// <param name="isRon">Indicates the human player won by ron (as opposed to tsumo); irrelevant if the human player didn't win.</param>
    /// <param name="humanIsRiichi">Indicates the human player was riichi during this round.</param>
    /// <param name="humanIsConcealed">Indicates the human player's hand was still concealed at the end of the round.</param>
    /// <param name="scoreIndexPosition">The human player's rank at the end of the game; irrelevant if the game isn't over.</param>
    /// <param name="meScore">The human player's current points; irrelevant if the game isn't over.</param>
    /// <returns>An error message if the save failed; <c>Null</c> otherwise.</returns>
    internal string? UpdateAndSave(EndOfRoundInformationsPivot endOfRoundInformations,
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

        // save at each round (so rounds on given up games are kept)
        return SavePlayerFile();
    }
}
