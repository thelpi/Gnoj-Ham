namespace Gnoj_Ham_ViewModel;

/// <summary>
/// One line of the ranking shown after a round.
/// </summary>
/// <param name="PlayerName">The player name.</param>
/// <param name="Gain">Points gained or lost during the round.</param>
/// <param name="Points">The player's points after the round.</param>
public sealed record ScoreRankingRowViewModel(string PlayerName, GainViewModel Gain, int Points);
