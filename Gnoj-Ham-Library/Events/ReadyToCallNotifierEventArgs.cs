using Gnoj_Ham_Library.Enums;

namespace Gnoj_Ham_Library.Events;

/// <summary>
/// Event triggered when a call is available.
/// </summary>
public class ReadyToCallNotifierEventArgs
{
    /// <summary>
    /// The type of call.
    /// </summary>
    public CallTypes Call { get; internal init; }

    /// <summary>
    /// Player index; usage only for call 'Pon'.
    /// </summary>
    public int PlayerIndex { get; internal init; }

    /// <summary>
    /// Previous player index; usage only for call 'Pon'.
    /// </summary>
    public int PreviousPlayerIndex { get; internal init; }

    /// <summary>
    /// Previous player index; usage only for call 'Kan''.
    /// </summary>
    public int? PotentialPreviousPlayerIndex { get; internal init; }
}
