namespace Gnoj_Ham_Library;

/// <summary>
/// Exact reproduction of <see cref="BasicCpuManagerPivot"/>'s original behavior, before the kabe
/// (wall) safety reading was extended from isolated honors to numbered tiles - kept only so
/// <see cref="CpuManagerCatalog"/> can benchmark that extension's actual impact on win rate (see
/// <see cref="BasicCpuManagerPivot.ReadsKabeForNumbers"/>) against the corrected version.
/// </summary>
public class BasicNoNumberKabeCpuManagerPivot : BasicCpuManagerPivot
{
    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="round">The <see cref="CpuManagerBasePivot.Round"/> value.</param>
    public BasicNoNumberKabeCpuManagerPivot(RoundPivot round)
        : base(round)
    { }

    protected override bool ReadsKabeForNumbers => false;
}
