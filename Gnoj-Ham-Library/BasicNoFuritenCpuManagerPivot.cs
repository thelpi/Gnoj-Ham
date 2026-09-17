namespace Gnoj_Ham_Library;

/// <summary>
/// Exact reproduction of <see cref="BasicCpuManagerPivot"/>'s original, furiten-blind behavior - kept
/// only so <see cref="CpuManagerCatalog"/> can benchmark the furiten-avoidance fix's actual impact on
/// win rate (see <see cref="BasicCpuManagerPivot.AvoidsFuriten"/>) against the corrected version.
/// </summary>
public class BasicNoFuritenCpuManagerPivot : BasicCpuManagerPivot
{
    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="round">The <see cref="CpuManagerBasePivot.Round"/> value.</param>
    public BasicNoFuritenCpuManagerPivot(RoundPivot round)
        : base(round)
    { }

    protected override bool AvoidsFuriten => false;
}
