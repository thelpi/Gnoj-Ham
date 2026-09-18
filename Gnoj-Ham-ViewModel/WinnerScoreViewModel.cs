using Gnoj_Ham_Library;

namespace Gnoj_Ham_ViewModel;

/// <summary>
/// What the score window shows for one winner: the hand, how it's valued, and what it brings.
/// </summary>
public sealed class WinnerScoreViewModel
{
    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="playerName">The winner's name.</param>
    /// <param name="info">The winner's information for this round.</param>
    public WinnerScoreViewModel(string playerName, EndOfRoundInformationsPivot.PlayerInformationsPivot info)
    {
        PlayerName = playerName;
        HandPointsGain = info.HandPointsGain;
        HasYakus = info.Yakus != null && info.Yakus.Count > 0;
        FanText = $"{info.FanCount} fan";
        FuText = $"{info.FuCount} fu";

        HandTiles = info.GetFullHandForDisplay()
            .Select(t => new TileViewModel(t.tile, t.isLeaned ? AnglePivot.A90 : AnglePivot.A0, isConcealed: false, isApart: t.isWinPick))
            .ToList();

        YakuLines = HasYakus ? BuildYakuLines(info) : Array.Empty<YakuLineViewModel>();
    }

    /// <summary>
    /// The winner's name.
    /// </summary>
    public string PlayerName { get; }

    /// <summary>
    /// The points brought by the hand alone (no honba, no riichi sticks).
    /// </summary>
    public int HandPointsGain { get; }

    /// <summary>
    /// Indicates if the hand has yakus (and so has a fan and fu breakdown to show).
    /// </summary>
    public bool HasYakus { get; }

    /// <summary>
    /// The fan count, ready to display.
    /// </summary>
    public string FanText { get; }

    /// <summary>
    /// The fu count, ready to display.
    /// </summary>
    public string FuText { get; }

    /// <summary>
    /// The full winning hand.
    /// </summary>
    public IReadOnlyList<TileViewModel> HandTiles { get; }

    /// <summary>
    /// The yakus and doras, with their fans; empty when the hand has no yakus.
    /// </summary>
    public IReadOnlyList<YakuLineViewModel> YakuLines { get; }

    private static IReadOnlyList<YakuLineViewModel> BuildYakuLines(EndOfRoundInformationsPivot.PlayerInformationsPivot info)
    {
        var lines = info.Yakus!
            .GroupBy(y => y)
            .Select(g => new YakuLineViewModel(g.Key.Name, (info.Concealed ? g.Key.ConcealedFanCount : g.Key.FanCount) * g.Count()))
            .ToList();

        if (info.DoraCount > 0)
        {
            lines.Add(new YakuLineViewModel(YakuPivot.Dora, info.DoraCount));
        }
        if (info.UraDoraCount > 0)
        {
            lines.Add(new YakuLineViewModel(YakuPivot.UraDora, info.UraDoraCount));
        }
        if (info.RedDoraCount > 0)
        {
            lines.Add(new YakuLineViewModel(YakuPivot.RedDora, info.RedDoraCount));
        }

        return lines;
    }
}
