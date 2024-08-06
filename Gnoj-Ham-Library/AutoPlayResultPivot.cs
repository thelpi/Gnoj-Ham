using Gnoj_Ham_Library.Enums;

namespace Gnoj_Ham_Library;

public class AutoPlayResultPivot
{
    private readonly Dictionary<PlayerIndices, CallTypes> _humanCalls = new();

    /// <summary>
    /// Indicates if the round is over; otherwise, the control is given back to the UI.
    /// </summary>
    public bool EndOfRound { get; internal set; }

    /// <summary>
    /// Indicates, if one or several calls 'Ron' has been made, the player index who lost in that situation.
    /// </summary>
    public PlayerIndices? RonPlayerId { get; internal set; }

    /// <summary>
    /// Indicates decisions to automatically apply when the control is given back to the UI.
    /// </summary>
    public IReadOnlyDictionary<PlayerIndices, CallTypes> HumanCalls => _humanCalls;

    /// <summary>
    /// Inserts a human call.
    /// </summary>
    /// <param name="playerIndex">Involved player index.</param>
    /// <param name="call">Call type.</param>
    internal void AddHumanCall(PlayerIndices playerIndex, CallTypes call)
    {
        _humanCalls[playerIndex] = call;
    }
}
