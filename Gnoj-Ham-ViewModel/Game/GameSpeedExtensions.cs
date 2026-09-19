namespace Gnoj_Ham_ViewModel;

/// <summary>
/// Converts the pacing settings into durations.
/// </summary>
public static class GameSpeedExtensions
{
    /// <summary>
    /// Extension; converts a <see cref="CpuSpeedPivot"/> to a integer value.
    /// </summary>
    /// <param name="cpuSpeed">The speed to convert.</param>
    /// <returns>The integer value, in milliseconds.</returns>
    public static int ParseSpeed(this CpuSpeedPivot cpuSpeed)
    {
        return Convert.ToInt32(cpuSpeed.ToString().Replace("S", string.Empty));
    }

    /// <summary>
    /// Transforms a <see cref="ChronoPivot"/> value into its delay in seconds.
    /// </summary>
    /// <param name="chrono">The chrono value.</param>
    /// <returns>Delay in seconds.</returns>
    public static int GetDelay(this ChronoPivot chrono)
    {
        return chrono switch
        {
            ChronoPivot.Long => 20,
            ChronoPivot.Short => 5,
            _ => 0,
        };
    }
}
