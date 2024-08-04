namespace Gnoj_Ham_Library.Events;

/// <summary>
/// Event triggered when the human can make an automatic call.
/// </summary>
public class HumanCallNotifierEventArgs : EventArgs
{
    /// <summary>
    /// Call type.
    /// </summary>
    public CallTypePivot Call { get; internal init; }

    /// <summary>
    /// In case of 'Riichi' call, indicates if the call is advised (if the advice feature is eanbled).
    /// </summary>
    public bool RiichiAdvised { get; internal init; }
}
