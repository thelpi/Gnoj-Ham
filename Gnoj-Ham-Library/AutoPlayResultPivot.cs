using Gnoj_Ham_Library.Enums;

namespace Gnoj_Ham_Library;

public class AutoPlayResultPivot
{
    /// <summary>
    /// Indicates if the round is over; otherwise, the control is given back to the UI.
    /// </summary>
    public bool EndOfRound { get; internal set; }

    /// <summary>
    /// Indicates, if one or several calls 'Ron' has been made, the player index who lost in that situation.
    /// </summary>
    public PlayerIndices? RonPlayerId { get; internal set; }

    /// <summary>
    /// Indicates decision to automatically apply when the control is given back to the UI.
    /// </summary>
    public (PlayerIndices playerIndex, CallTypes call)? HumanCall { get; internal set; }
}
