using System.Media;
using Gnoj_Ham_View.Properties;
using Gnoj_Ham_ViewModel.Services;

namespace Gnoj_Ham_View.Services;

/// <summary>
/// Plays the game's sounds, kept in the application resources.
/// </summary>
internal sealed class WpfSoundService : ISoundService
{
    private readonly SoundPlayer _tick = new(Resources.tick);

    /// <inheritdoc />
    public void PlayTick() => _tick.Play();
}
