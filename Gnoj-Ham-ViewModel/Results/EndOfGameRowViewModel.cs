namespace Gnoj_Ham_ViewModel;

/// <summary>
/// One line of the final ranking.
/// </summary>
/// <param name="Rank">The rank.</param>
/// <param name="PlayerName">The player name.</param>
/// <param name="Points">The final points.</param>
/// <param name="Uma">The uma bonus or malus.</param>
/// <param name="Score">The final score.</param>
public sealed record EndOfGameRowViewModel(int Rank, string PlayerName, int Points, GainViewModel Uma, GainViewModel Score);
