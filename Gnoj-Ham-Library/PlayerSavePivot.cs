using System.Text.Json;

namespace Gnoj_Ham_Library;

// TODO: this class is an abomination
/// <summary>
/// Player save file
/// </summary>
[Serializable]
public class PlayerSavePivot
{
    private const string SAVE_FILE_NAME = "save_file.dat";

    private static string FullFileName => $"{Environment.CurrentDirectory}\\{SAVE_FILE_NAME}";

    /// <summary>
    /// A marker to avoid double count.
    /// </summary>
    private bool InProgressGame { get; set; }
    /// <summary>
    /// Date of the first game (with at least one round completed).
    /// </summary>
    public DateTime? FirstGame { get; private set; }
    /// <summary>
    /// Date of the most recent completed game (with at least one round completed).
    /// </summary>
    public DateTime? LastGame { get; private set; }
    /// <summary>
    /// Number of games.
    /// </summary>
    public int GameCount { get; private set; }
    /// <summary>
    /// Number of rounds.
    /// </summary>
    public int RoundCount { get; private set; }
    /// <summary>
    /// Number of games by ranking position.
    /// </summary>
    public int[] ByPositionCount { get; private set; } = new[] { 0, 0, 0, 0 };
    /// <summary>
    /// Number of riichi declarations.
    /// </summary>
    public int RiichiCount { get; private set; }
    /// <summary>
    /// Number of bankrupts.
    /// </summary>
    public int BankruptCount { get; private set; }
    /// <summary>
    /// Number of tsumo declarations.
    /// </summary>
    public int TsumoCount { get; private set; }
    /// <summary>
    /// Number of ron declarations.
    /// </summary>
    public int RonCount { get; private set; }
    /// <summary>
    /// Number of yakuman hands.
    /// </summary>
    public int YakumanCount { get; private set; }
    /// <summary>
    /// Number of opened hands.
    /// </summary>
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
                // TODO decrypt
                using var stream = new FileStream(FullFileName, FileMode.Open, FileAccess.Read, FileShare.Read);
                save = JsonSerializer.Deserialize<PlayerSavePivot>(stream)!;
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
    /// 
    /// </summary>
    /// <param name="endOfRoundInformations"></param>
    /// <param name="isRon"></param>
    /// <param name="humanIsRiichi"></param>
    /// <param name="humanIsConcealed"></param>
    /// <param name="scoreIndexPosition"></param>
    /// <param name="meScore"></param>
    /// <returns></returns>
    internal string? UpdateAndSave(EndOfRoundInformationsPivot endOfRoundInformations,
        bool isRon, bool humanIsRiichi, bool humanIsConcealed, int scoreIndexPosition, int meScore)
    {
        var now = DateTime.Now;
        var pHand = endOfRoundInformations.PlayersInfo?.FirstOrDefault(_ => _.Index == GamePivot.HUMAN_INDEX);

        if (!InProgressGame)
        {
            InProgressGame = true;
        }

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
            InProgressGame = false;
        }

        // save at each round (so rounds on given up games are kept)
        return SavePlayerFile();
    }
}
