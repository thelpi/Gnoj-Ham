namespace Gnoj_Ham_ViewModel;

/// <summary>
/// One line of a winning hand's breakdown: a yaku (or a dora kind) and the fans it brings.
/// </summary>
/// <param name="Name">The yaku or dora name.</param>
/// <param name="FanCount">The fans it brings.</param>
public sealed record YakuLineViewModel(string Name, int FanCount);
