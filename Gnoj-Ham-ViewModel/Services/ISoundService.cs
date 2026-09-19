namespace Gnoj_Ham_ViewModel.Services;

/// <summary>
/// Plays the game's sounds. Whether sounds are enabled is the caller's business.
/// </summary>
public interface ISoundService
{
    /// <summary>
    /// Plays the sound of a tile being picked.
    /// </summary>
    void PlayTick();
}
