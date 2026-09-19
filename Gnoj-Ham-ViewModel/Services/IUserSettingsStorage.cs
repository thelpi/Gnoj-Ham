namespace Gnoj_Ham_ViewModel.Services;

/// <summary>
/// Reads and writes the user's settings from/to wherever they are saved.
/// </summary>
public interface IUserSettingsStorage
{
    /// <summary>
    /// Loads the settings; those not saved yet, or that cannot be read, have their default value.
    /// </summary>
    /// <returns>The settings.</returns>
    UserSettings Load();

    /// <summary>
    /// Saves the settings. Failing to is not an error worth stopping the game for: they are just not kept.
    /// </summary>
    /// <param name="settings">The settings.</param>
    void Save(UserSettings settings);
}
