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
        return cpuSpeed switch
        {
            CpuSpeedPivot.S2000 => 2000,
            CpuSpeedPivot.S1000 => 1000,
            CpuSpeedPivot.S500 => 500,
            CpuSpeedPivot.S200 => 200,
            CpuSpeedPivot.S0 => 0,
            _ => throw new ArgumentOutOfRangeException(nameof(cpuSpeed), cpuSpeed, null),
        };
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
