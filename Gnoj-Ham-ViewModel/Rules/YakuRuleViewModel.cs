using Gnoj_Ham_Library;

namespace Gnoj_Ham_ViewModel;

/// <summary>
/// A yaku as the rules list it: its name, worth, description and an example of a hand that makes it.
/// </summary>
public sealed class YakuRuleViewModel
{
    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="yaku">The yaku.</param>
    public YakuRuleViewModel(YakuPivot yaku)
    {
        Name = yaku.Name;
        Description = yaku.Description;
        IsConcealedOnly = yaku.IsConcealedOnly;
        FansText = yaku.FanCount > 0 && yaku.ConcealedBonusFanCount > 0
            ? $"{yaku.FanCount} (+{yaku.ConcealedBonusFanCount})"
            : yaku.ConcealedFanCount.ToString();

        if (yaku.ConcealedFanCount == 13 && yaku.FanCount == 0)
        {
            ToolTip = "Yakuman. Main fermée uniquement.";
        }
        else if (yaku.FanCount == 0)
        {
            ToolTip = "Main fermée uniquement.";
        }
        else if (yaku.ConcealedBonusFanCount > 0)
        {
            ToolTip = "Bonus si main fermée.";
        }

        ExampleTiles = yaku.Example?.Select(t => new TileViewModel(t)).ToList() ?? (IReadOnlyList<TileViewModel>)Array.Empty<TileViewModel>();
    }

    /// <summary>
    /// The yaku name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// The yaku description.
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// The worth in fans: a single number, or the worth of an open hand and what a concealed one adds.
    /// </summary>
    public string FansText { get; }

    /// <summary>
    /// A note on the conditions of the yaku; empty when it has none.
    /// </summary>
    public string ToolTip { get; } = string.Empty;

    /// <summary>
    /// Indicates if the yaku can only be made with a concealed hand.
    /// </summary>
    public bool IsConcealedOnly { get; }

    /// <summary>
    /// An example of a hand that makes the yaku; empty when there is none.
    /// </summary>
    public IReadOnlyList<TileViewModel> ExampleTiles { get; }
}
