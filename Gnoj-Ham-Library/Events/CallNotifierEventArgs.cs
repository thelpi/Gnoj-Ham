namespace Gnoj_Ham_Library.Events;

/// <summary>
/// Event triggered when a call has to be notified.
/// </summary>
public class CallNotifierEventArgs : EventArgs
{
    /// <summary>
    /// The player index.
    /// </summary>
    public int PlayerIndex { get; internal init; }

    /// <summary>
    /// The type of call.
    /// </summary>
    public CallTypePivot Action { get; internal init; }
}
