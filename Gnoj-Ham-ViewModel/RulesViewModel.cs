using Gnoj_Ham_Library;

namespace Gnoj_Ham_ViewModel;

/// <summary>
/// The rules and glossary window's data: the list of yakus.
/// </summary>
public sealed class RulesViewModel
{
    /// <summary>
    /// Constructor.
    /// </summary>
    public RulesViewModel()
    {
        Yakus = YakuPivot.Yakus
            .Except(new[] { YakuPivot.NagashiMangan })
            .OrderBy(x => x.ConcealedFanCount)
            .ThenBy(x => x.FanCount)
            .ThenBy(x => x.Name)
            .Select(y => new YakuRuleViewModel(y))
            .ToList();
    }

    /// <summary>
    /// The yakus, from the least to the most valuable (nagashi mangan excluded: it isn't a regular hand).
    /// </summary>
    public IReadOnlyList<YakuRuleViewModel> Yakus { get; }
}
