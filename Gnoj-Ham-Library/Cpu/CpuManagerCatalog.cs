namespace Gnoj_Ham_Library;

/// <summary>
/// Every concrete <see cref="CpuManagerBasePivot"/> implementation available to pick from - e.g. for a
/// UI seat picker, or to benchmark one against three others over many unattended games (see
/// <see cref="GamePivot"/>'s <c>cpuManagerFactories</c> constructor parameter) - alongside a short,
/// hardcoded display name for each (e.g. to tag a player's name with which logic is actually playing
/// them). Add a new entry here whenever a new <see cref="CpuManagerBasePivot"/> implementation is
/// written.
/// </summary>
public static class CpuManagerCatalog
{
    /// <summary>
    /// The implementation that plays every CPU seat nobody picked another for: the best one measured so
    /// far (see <see cref="CreateDefault"/>, to keep in step with it).
    /// </summary>
    public static readonly CpuManagerOption Default = new(typeof(EfficiencyCallsCpuManagerPivot), "efficiency calls");

    /// <summary>
    /// Every available implementation, in a fixed, deliberate order (not discovered via reflection): the
    /// <see cref="Default"/> first, then the ones it is built on, then the variants kept to be measured against.
    /// </summary>
    public static readonly IReadOnlyList<CpuManagerOption> Implementations = new List<CpuManagerOption>
    {
        Default,
        new(typeof(EfficiencyPushFoldCpuManagerPivot), "efficiency push fold"),
        new(typeof(EfficiencyCpuManagerPivot), "efficiency"),
        new(typeof(BasicCpuManagerPivot), "basic"),
        new(typeof(NoDefenseCpuManagerPivot), "no defense"),
        new(typeof(FullDefenseCpuManagerPivot), "full defense"),
        new(typeof(BasicNoFuritenCpuManagerPivot), "basic no furiten"),
        new(typeof(BasicNoWaitWidthCpuManagerPivot), "basic no wait width"),
        new(typeof(BasicNoNumberKabeCpuManagerPivot), "basic no number kabe"),
    };

    /// <summary>
    /// Creates the <see cref="Default"/> implementation.
    /// </summary>
    /// <param name="round">The <see cref="CpuManagerBasePivot.Round"/> value.</param>
    /// <returns>The CPU manager.</returns>
    internal static CpuManagerBasePivot CreateDefault(RoundPivot round)
        => new EfficiencyCallsCpuManagerPivot(round);
}
