using System.IO;
using System.Text.Json;
using Gnoj_Ham_Library;

namespace Gnoj_Ham_View;

/// <summary>
/// Reads and writes <see cref="PlayerStatisticsPivot"/> to/from the local save file. Pure file I/O -
/// knows nothing about mahjong.
/// </summary>
internal static class PlayerSaveStorage
{
    private const string SAVE_FILE_NAME = "save_file.dat";

    private static string FullFileName => Path.Combine(Environment.CurrentDirectory, SAVE_FILE_NAME);

    /// <summary>
    /// Loads the player statistics from the save file, or creates a fresh instance if it doesn't exist yet.
    /// </summary>
    /// <returns>Player statistics, and an error message if the load failed.</returns>
    public static (PlayerStatisticsPivot stats, string? error) Load()
    {
        var stats = new PlayerStatisticsPivot();

        try
        {
            if (File.Exists(FullFileName))
            {
                using var stream = new FileStream(FullFileName, FileMode.Open, FileAccess.Read, FileShare.Read);
                stats = JsonSerializer.Deserialize<PlayerStatisticsPivot>(stream)
                    ?? throw new InvalidOperationException("Le fichier de sauvegarde est vide ou invalide.");
            }
        }
        catch (Exception ex)
        {
            return (stats, ex.Message);
        }

        return (stats, null);
    }

    /// <summary>
    /// Saves the player statistics to the save file.
    /// </summary>
    /// <param name="stats">Player statistics.</param>
    /// <returns>An error message if the save failed; <c>Null</c> otherwise.</returns>
    public static string? Save(PlayerStatisticsPivot stats)
    {
        try
        {
            using var stream = new FileStream(FullFileName, FileMode.Create, FileAccess.Write, FileShare.None);
            JsonSerializer.Serialize(stream, stats);
        }
        catch (Exception ex)
        {
            return ex.Message;
        }

        return null;
    }
}
