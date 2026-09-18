namespace Gnoj_Ham_ViewModel;

/// <summary>
/// Where an auto-play batch is at.
/// </summary>
public enum AutoPlayState
{
    /// <summary>
    /// Nothing has been started yet.
    /// </summary>
    Idle,
    /// <summary>
    /// Games are being played.
    /// </summary>
    Running,
    /// <summary>
    /// Every game is over and the results are available.
    /// </summary>
    Finished
}
