using Gnoj_Ham_Library;

namespace Gnoj_Ham_ViewModel;

/// <summary>
/// The final ranking shown once the game is over.
/// </summary>
public sealed class EndOfGameViewModel
{
    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="scores">The players' final scores, best rank first.</param>
    public EndOfGameViewModel(IReadOnlyList<PlayerScorePivot> scores)
    {
        Rows = scores
            .Select(s => new EndOfGameRowViewModel(
                s.Rank,
                s.Player.Name,
                s.Player.CurrentGamePoints,
                new GainViewModel(s.Uma),
                new GainViewModel(s.Score)))
            .ToList();
    }

    /// <summary>
    /// The ranking, one row per player.
    /// </summary>
    public IReadOnlyList<EndOfGameRowViewModel> Rows { get; }
}
