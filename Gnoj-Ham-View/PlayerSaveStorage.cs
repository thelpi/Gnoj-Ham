using System.IO;
using System.Security.Cryptography;
using System.Text.Json;
using Gnoj_Ham_Library;

namespace Gnoj_Ham_View;

/// <summary>
/// Reads and writes <see cref="PlayerStatisticsPivot"/> to/from the local save file, encrypted with
/// Windows DPAPI (tied to the current Windows user account - no key management needed). Pure file
/// I/O - knows nothing about mahjong.
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
                var fileBytes = File.ReadAllBytes(FullFileName);

                byte[] jsonBytes;
                try
                {
                    jsonBytes = ProtectedData.Unprotect(fileBytes, null, DataProtectionScope.CurrentUser);
                }
                catch (CryptographicException)
                {
                    // Pre-encryption save file (plain JSON): read as-is; Save will re-encrypt it next time.
                    jsonBytes = fileBytes;
                }

                stats = JsonSerializer.Deserialize<PlayerStatisticsPivot>(jsonBytes)
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
    /// Saves the player statistics to the save file, encrypted.
    /// </summary>
    /// <param name="stats">Player statistics.</param>
    /// <returns>An error message if the save failed; <c>Null</c> otherwise.</returns>
    public static string? Save(PlayerStatisticsPivot stats)
    {
        try
        {
            var jsonBytes = JsonSerializer.SerializeToUtf8Bytes(stats);
            var encryptedBytes = ProtectedData.Protect(jsonBytes, null, DataProtectionScope.CurrentUser);
            File.WriteAllBytes(FullFileName, encryptedBytes);
        }
        catch (Exception ex)
        {
            return ex.Message;
        }

        return null;
    }
}
