namespace Gnoj_Ham_Library;

/// <summary>
/// Exact reproduction of <see cref="BasicCpuManagerPivot"/>'s original, wait-width-blind behavior -
/// kept only so <see cref="CpuManagerCatalog"/> can benchmark the wait-width tie-break's actual impact
/// on win rate (see <see cref="BasicCpuManagerPivot.PrefersLiveWait"/>) against the corrected version.
/// </summary>
public class BasicNoWaitWidthCpuManagerPivot : BasicCpuManagerPivot
{
    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="round">The <see cref="CpuManagerBasePivot.Round"/> value.</param>
    public BasicNoWaitWidthCpuManagerPivot(RoundPivot round)
        : base(round)
    { }

    protected override bool PrefersLiveWait => false;
}
