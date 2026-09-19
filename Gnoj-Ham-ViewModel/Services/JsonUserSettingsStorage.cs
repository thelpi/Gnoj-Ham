using System.Text.Json;

namespace Gnoj_Ham_ViewModel.Services;

/// <summary>
/// Keeps the user's settings in a JSON file. Pure file I/O, with nothing specific to a UI technology.
/// </summary>
public sealed class JsonUserSettingsStorage : IUserSettingsStorage
{
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

    private readonly string _filePath;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="filePath">The file the settings are kept in; its folder is created when saving if need be.</param>
    public JsonUserSettingsStorage(string filePath)
    {
        _filePath = filePath;
    }

    /// <inheritdoc />
    public UserSettings Load()
    {
        try
        {
            if (File.Exists(_filePath))
            {
                return JsonSerializer.Deserialize<UserSettings>(File.ReadAllText(_filePath)) ?? new UserSettings();
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            // Unreadable settings are as good as none.
        }

        return new UserSettings();
    }

    /// <inheritdoc />
    public void Save(UserSettings settings)
    {
        try
        {
            var folder = Path.GetDirectoryName(_filePath);
            if (!string.IsNullOrEmpty(folder))
            {
                Directory.CreateDirectory(folder);
            }

            File.WriteAllText(_filePath, JsonSerializer.Serialize(settings, Options));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // The settings are just not kept.
        }
    }
}
