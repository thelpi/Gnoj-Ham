namespace Gnoj_Ham_Library;

/// <summary>
/// One entry of <see cref="CpuManagerCatalog.Implementations"/>: a concrete
/// <see cref="CpuManagerBasePivot"/> implementation paired with its short display name. A plain class
/// with real properties (rather than a tuple) so it binds cleanly to a WPF <c>DisplayMemberPath</c>.
/// </summary>
/// <param name="Type">The concrete <see cref="CpuManagerBasePivot"/> implementation type.</param>
/// <param name="DisplayName">Short, hardcoded display name (e.g. for a UI picker or a player's name).</param>
public sealed record CpuManagerOption(Type Type, string DisplayName);

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
    /// Every available implementation, in a fixed, deliberate order (not discovered via reflection).
    /// </summary>
    public static readonly IReadOnlyList<CpuManagerOption> Implementations = new List<CpuManagerOption>
    {
        new(typeof(BasicCpuManagerPivot), "basic"),
        new(typeof(NoDefenseCpuManagerPivot), "no defense"),
    };
}
